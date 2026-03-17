using ClosedXML.Excel;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Collections.Concurrent;
using System.Globalization;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Upload
{
    [JsonSchema("UploadPatientCommand")]
    public class Command : IRequest<Response>
    {
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private static readonly string[] SupportedHeaders =
        [
            "FirstName",
            "LastName",
            "MiddleName",
            "EmailAddress",
            "BirthDate",
            "ContactNumber",
            "Address",
            "Suffix",
            "Occupation",
            "Religion",
            "BloodType",
            "CivilStatus",
            "ProfilePicture",
        ];

        private readonly DmdDbContext dbContext;

        public CommandHandler(DmdDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.FileContent.Length == 0)
                {
                    return new BadRequestResponse("No file uploaded.");
                }

                var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return new BadRequestResponse("Invalid file type. Only .xlsx files are allowed.");
                }

                using var stream = new MemoryStream(request.FileContent);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("Patients", StringComparison.OrdinalIgnoreCase))
                    ?? workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return new BadRequestResponse("The uploaded workbook does not contain any worksheets.");
                }

                var firstUsedRow = worksheet.FirstRowUsed();
                if (firstUsedRow == null)
                {
                    return new BadRequestResponse("The uploaded worksheet is empty.");
                }

                var headerMap = BuildHeaderMap(firstUsedRow);
                if (!headerMap.ContainsKey("FirstName") || !headerMap.ContainsKey("LastName"))
                {
                    return new BadRequestResponse("The worksheet must contain FirstName and LastName columns.");
                }

                var result = new PatientUploadResultModel();
                var today = DateTime.Today;
                var sequence = await dbContext.PatientInfos.CountAsync(x => x.CreatedAt.Date == today, cancellationToken);
                var uploadRows = worksheet.RowsUsed()
                    .Skip(1)
                    .Select(row => BuildUploadRow(row, headerMap))
                    .Where(row => !IsEmptyRow(row))
                    .ToList();

                result.TotalRows = uploadRows.Count;

                var existingPatients = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Select(x => new
                    {
                        x.FirstName,
                        x.LastName,
                        x.MiddleName,
                        x.BirthDate,
                        x.EmailAddress,
                        x.ContactNumber,
                    })
                    .ToListAsync(cancellationToken);

                var existingIdentityKeys = existingPatients
                    .Select(x => BuildIdentityKey(x.FirstName, x.LastName, x.MiddleName, x.BirthDate))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existingEmailKeys = existingPatients
                    .Select(x => NormalizeValue(x.EmailAddress))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existingContactKeys = existingPatients
                    .Select(x => NormalizeContact(x.ContactNumber))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var validRows = new ConcurrentBag<ValidatedUploadRow>();
                var errors = new ConcurrentBag<string>();
                var skippedCount = 0;
                var fileIdentityKeys = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
                var fileEmailKeys = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
                var fileContactKeys = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

                Parallel.ForEach(uploadRows, row =>
                {
                    var firstName = NormalizeValue(row.FirstName);
                    var lastName = NormalizeValue(row.LastName);
                    var middleName = NormalizeValue(row.MiddleName);

                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        errors.Add($"Row {row.RowNumber}: FirstName and LastName are required.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!TryParseBirthDate(row.BirthDate, out var birthDate, out var birthDateError))
                    {
                        errors.Add($"Row {row.RowNumber}: {birthDateError}");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!TryParseEnum(row.Suffix, Suffix.None, out Suffix suffix))
                    {
                        errors.Add($"Row {row.RowNumber}: Invalid Suffix value.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!TryParseEnum(row.CivilStatus, CivilStatus.None, out CivilStatus civilStatus))
                    {
                        errors.Add($"Row {row.RowNumber}: Invalid CivilStatus value.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!TryParseEnum(row.BloodType, BloodTypes.A_Positive, out BloodTypes bloodType))
                    {
                        errors.Add($"Row {row.RowNumber}: Invalid BloodType value.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!IsValidEmail(row.EmailAddress))
                    {
                        errors.Add($"Row {row.RowNumber}: Invalid EmailAddress value.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    var identityKey = BuildIdentityKey(firstName, lastName, middleName, birthDate);
                    var emailKey = NormalizeValue(row.EmailAddress);
                    var contactKey = NormalizeContact(row.ContactNumber);

                    if (!string.IsNullOrWhiteSpace(identityKey) &&
                        (existingIdentityKeys.Contains(identityKey) || !fileIdentityKeys.TryAdd(identityKey, 0)))
                    {
                        errors.Add($"Row {row.RowNumber}: Patient already exists based on name and birth date.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(emailKey) &&
                        (existingEmailKeys.Contains(emailKey) || !fileEmailKeys.TryAdd(emailKey, 0)))
                    {
                        errors.Add($"Row {row.RowNumber}: Patient already exists based on email address.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(contactKey) &&
                        (existingContactKeys.Contains(contactKey) || !fileContactKeys.TryAdd(contactKey, 0)))
                    {
                        errors.Add($"Row {row.RowNumber}: Patient already exists based on contact number.");
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    validRows.Add(new ValidatedUploadRow
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        MiddleName = middleName,
                        EmailAddress = NormalizeValue(row.EmailAddress),
                        BirthDate = birthDate,
                        ContactNumber = NormalizeValue(row.ContactNumber),
                        Address = NormalizeValue(row.Address),
                        Suffix = suffix,
                        Occupation = NormalizeValue(row.Occupation),
                        Religion = NormalizeValue(row.Religion),
                        BloodType = bloodType,
                        CivilStatus = civilStatus,
                        ProfilePicture = NormalizeValue(row.ProfilePicture),
                        RowNumber = row.RowNumber,
                    });
                });

                var patientsToCreate = validRows
                    .OrderBy(x => x.RowNumber)
                    .Select(row =>
                    {
                        sequence++;
                        return new PatientInfo
                        {
                            PatientNumber = $"DMD-{today:yyyyMMdd}-{sequence:D4}",
                            FirstName = row.FirstName,
                            LastName = row.LastName,
                            MiddleName = row.MiddleName,
                            EmailAddress = row.EmailAddress,
                            BirthDate = row.BirthDate,
                            ContactNumber = row.ContactNumber,
                            Address = row.Address,
                            Suffix = row.Suffix,
                            Occupation = row.Occupation,
                            Religion = row.Religion,
                            BloodType = row.BloodType,
                            CivilStatus = row.CivilStatus,
                            ProfilePicture = row.ProfilePicture,
                        };
                    })
                    .ToList();

                if (patientsToCreate.Count > 0)
                {
                    dbContext.PatientInfos.AddRange(patientsToCreate);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                result.ImportedCount = patientsToCreate.Count;
                result.SkippedCount = skippedCount;
                result.Errors = errors.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                return new SuccessResponse<PatientUploadResultModel>(result);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
            finally
            {
                await dbContext.DisposeAsync();
            }
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                var headerName = cell.GetString().Trim();
                if (!string.IsNullOrWhiteSpace(headerName) && SupportedHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase))
                {
                    map[headerName] = cell.Address.ColumnNumber;
                }
            }

            return map;
        }

        private static UploadRow BuildUploadRow(IXLRow row, Dictionary<string, int> headerMap)
        {
            return new UploadRow
            {
                RowNumber = row.RowNumber(),
                FirstName = GetCellString(row, headerMap, "FirstName"),
                LastName = GetCellString(row, headerMap, "LastName"),
                MiddleName = GetCellString(row, headerMap, "MiddleName"),
                EmailAddress = GetCellString(row, headerMap, "EmailAddress"),
                BirthDate = GetCellString(row, headerMap, "BirthDate"),
                ContactNumber = GetCellString(row, headerMap, "ContactNumber"),
                Address = GetCellString(row, headerMap, "Address"),
                Suffix = GetCellString(row, headerMap, "Suffix"),
                Occupation = GetCellString(row, headerMap, "Occupation"),
                Religion = GetCellString(row, headerMap, "Religion"),
                BloodType = GetCellString(row, headerMap, "BloodType"),
                CivilStatus = GetCellString(row, headerMap, "CivilStatus"),
            };
        }

        private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string headerName)
        {
            if (!headerMap.TryGetValue(headerName, out var columnNumber))
            {
                return string.Empty;
            }

            return row.Cell(columnNumber).GetString().Trim();
        }

        private static bool IsEmptyRow(UploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.FirstName)
                && string.IsNullOrWhiteSpace(row.LastName)
                && string.IsNullOrWhiteSpace(row.MiddleName)
                && string.IsNullOrWhiteSpace(row.EmailAddress)
                && string.IsNullOrWhiteSpace(row.BirthDate)
                && string.IsNullOrWhiteSpace(row.ContactNumber)
                && string.IsNullOrWhiteSpace(row.Address)
                && string.IsNullOrWhiteSpace(row.Suffix)
                && string.IsNullOrWhiteSpace(row.Occupation)
                && string.IsNullOrWhiteSpace(row.Religion)
                && string.IsNullOrWhiteSpace(row.BloodType)
                && string.IsNullOrWhiteSpace(row.CivilStatus)
                && string.IsNullOrWhiteSpace(row.ProfilePicture);
        }

        private static bool TryParseBirthDate(
            string rawValue,
            out DateTime? birthDate,
            out string errorMessage)
        {
            birthDate = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
            {
                birthDate = parsedDate.Date;
                return true;
            }

            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate))
            {
                try
                {
                    birthDate = DateTime.FromOADate(oaDate).Date;
                    return true;
                }
                catch
                {
                }
            }

            errorMessage = "BirthDate must be a valid Excel date or YYYY-MM-DD string.";
            return false;
        }

        private static bool TryParseEnum<TEnum>(string rawValue, TEnum defaultValue, out TEnum parsedValue)
            where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                parsedValue = defaultValue;
                return true;
            }

            return Enum.TryParse(rawValue, true, out parsedValue);
        }

        private static string NormalizeValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeContact(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static string BuildIdentityKey(string? firstName, string? lastName, string? middleName, DateTime? birthDate)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return string.Empty;
            }

            return string.Join("|",
                NormalizeValue(firstName).ToUpperInvariant(),
                NormalizeValue(lastName).ToUpperInvariant(),
                NormalizeValue(middleName).ToUpperInvariant(),
                birthDate?.Date.ToString("yyyy-MM-dd") ?? string.Empty);
        }

        private static bool IsValidEmail(string? emailAddress)
        {
            var value = NormalizeValue(emailAddress);
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            try
            {
                _ = new System.Net.Mail.MailAddress(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class UploadRow
        {
            public int RowNumber { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            public string BirthDate { get; set; } = string.Empty;
            public string ContactNumber { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Suffix { get; set; } = string.Empty;
            public string Occupation { get; set; } = string.Empty;
            public string Religion { get; set; } = string.Empty;
            public string BloodType { get; set; } = string.Empty;
            public string CivilStatus { get; set; } = string.Empty;
            public string ProfilePicture { get; set; } = string.Empty;
        }

        private class ValidatedUploadRow
        {
            public int RowNumber { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            public DateTime? BirthDate { get; set; }
            public string ContactNumber { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public Suffix Suffix { get; set; }
            public string Occupation { get; set; } = string.Empty;
            public string Religion { get; set; } = string.Empty;
            public BloodTypes BloodType { get; set; }
            public CivilStatus CivilStatus { get; set; }
            public string ProfilePicture { get; set; } = string.Empty;
        }
    }
}
