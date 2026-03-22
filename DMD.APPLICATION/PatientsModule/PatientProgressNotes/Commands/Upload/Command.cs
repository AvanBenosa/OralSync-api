using ClosedXML.Excel;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Patients;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace DMD.APPLICATION.PatientsModule.PatientProgressNotes.Commands.Upload
{
    [JsonSchema("UploadPatientProgressNoteCommand")]
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
            "AssignedDoctor",
            "Date",
            "NextVisit",
            "Procedure",
            "Category",
            "Remarks",
            "ClinicalFinding",
            "Assessment",
            "ToothNumber",
            "Balance",
            "Account",
            "Amount",
            "Discount",
            "TotalAmountDue",
            "AmountPaid",
        ];

        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandHandler(DmdDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.FileContent.Length == 0)
                {
                    return new BadRequestResponse("No file uploaded.");
                }

                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                if (!int.TryParse(clinicIdValue, out var clinicId))
                {
                    return new BadRequestResponse("Clinic registration must be completed before importing progress notes.");
                }

                var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".csv")
                {
                    return new BadRequestResponse("Invalid file type. Only .xlsx and .csv files are allowed.");
                }

                var uploadRowsResult = extension == ".csv"
                    ? BuildUploadRowsFromCsv(request.FileContent)
                    : BuildUploadRowsFromExcel(request.FileContent);

                if (!uploadRowsResult.Success)
                {
                    return new BadRequestResponse(uploadRowsResult.ErrorMessage);
                }

                var uploadRows = uploadRowsResult.Rows;
                var result = new PatientUploadResultModel
                {
                    TotalRows = uploadRows.Count
                };

                var patients = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId)
                    .Select(x => new PatientLookupItem
                    {
                        Id = x.Id,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        MiddleName = x.MiddleName,
                    })
                    .ToListAsync(cancellationToken);

                var patientLookup = patients
                    .GroupBy(x => BuildShortNameKey(x.LastName, x.FirstName), StringComparer.OrdinalIgnoreCase)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                    .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

                var patientExactLookup = patients
                    .GroupBy(
                        x => BuildExactNameKey(x.LastName, x.FirstName, x.MiddleName),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                    .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

                var itemsToCreate = new List<PatientProgressNote>();
                var errors = new List<string>();
                var skippedCount = 0;

                foreach (var row in uploadRows.OrderBy(x => x.RowNumber))
                {
                    var firstName = NormalizeValue(row.FirstName);
                    var lastName = NormalizeValue(row.LastName);
                    var middleName = NormalizeValue(row.MiddleName);

                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        errors.Add($"Row {row.RowNumber}: LastName and FirstName are required.");
                        skippedCount++;
                        continue;
                    }

                    var matchedPatients = ResolvePatientMatches(
                        patientLookup,
                        patientExactLookup,
                        lastName,
                        firstName,
                        middleName);

                    if (matchedPatients.Count == 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (matchedPatients.Count > 1)
                    {
                        errors.Add($"Row {row.RowNumber}: Multiple PatientInfo records matched {FormatPatientLabel(lastName, firstName, middleName)}.");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseDate(row.Date, required: true, out var noteDate, out var dateError))
                    {
                        errors.Add($"Row {row.RowNumber}: {dateError}");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseDate(row.NextVisit, required: false, out var nextVisit, out var nextVisitError))
                    {
                        errors.Add($"Row {row.RowNumber}: {nextVisitError}");
                        skippedCount++;
                        continue;
                    }

                    var procedure = NormalizeValue(row.Procedure);
                    if (string.IsNullOrWhiteSpace(procedure))
                    {
                        errors.Add($"Row {row.RowNumber}: Procedure is required.");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalWholeNumber(row.ToothNumber, out var toothNumber, out var toothNumberError))
                    {
                        errors.Add($"Row {row.RowNumber}: {toothNumberError}");
                        skippedCount++;
                        continue;
                    }

                    if (toothNumber.HasValue && (toothNumber.Value < 1 || toothNumber.Value > 32))
                    {
                        errors.Add($"Row {row.RowNumber}: ToothNumber must be a whole number from 1 to 32.");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalNumber(row.Amount, out var amount, out var amountError))
                    {
                        errors.Add($"Row {row.RowNumber}: {amountError}");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalNumber(row.Discount, out var discount, out var discountError))
                    {
                        errors.Add($"Row {row.RowNumber}: {discountError}");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalNumber(row.TotalAmountDue, out var totalAmountDue, out var totalAmountDueError))
                    {
                        errors.Add($"Row {row.RowNumber}: {totalAmountDueError}");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalNumber(row.AmountPaid, out var amountPaid, out var amountPaidError))
                    {
                        errors.Add($"Row {row.RowNumber}: {amountPaidError}");
                        skippedCount++;
                        continue;
                    }

                    if (!TryParseOptionalNumber(row.Balance, out var balance, out var balanceError))
                    {
                        errors.Add($"Row {row.RowNumber}: {balanceError}");
                        skippedCount++;
                        continue;
                    }

                    if (amount.HasValue && amount.Value < 0)
                    {
                        errors.Add($"Row {row.RowNumber}: Amount cannot be negative.");
                        skippedCount++;
                        continue;
                    }

                    if (discount.HasValue && discount.Value < 0)
                    {
                        errors.Add($"Row {row.RowNumber}: Discount cannot be negative.");
                        skippedCount++;
                        continue;
                    }

                    if (amountPaid.HasValue && amountPaid.Value < 0)
                    {
                        errors.Add($"Row {row.RowNumber}: AmountPaid cannot be negative.");
                        skippedCount++;
                        continue;
                    }

                    var resolvedDiscount = discount ?? 0;
                    var resolvedAmount = amount ?? 0;
                    var resolvedTotalAmountDue = totalAmountDue ?? 0;
                    var resolvedAmountPaid = amountPaid ?? 0;
                    var resolvedBalance = balance ?? 0;
                    var matchedPatient = matchedPatients[0];

                    itemsToCreate.Add(new PatientProgressNote
                    {
                        PatientInfoId = matchedPatient.Id,
                        AssignedDoctor = NormalizeValue(row.AssignedDoctor),
                        Date = noteDate,
                        NextVisit = nextVisit,
                        Procedure = procedure,
                        Category = NormalizeValue(row.Category),
                        Remarks = NormalizeValue(row.Remarks),
                        ClinicalFinding = NormalizeValue(row.ClinicalFinding),
                        Assessment = NormalizeValue(row.Assessment),
                        ToothNumber = toothNumber,
                        Balance = resolvedBalance,
                        Account = NormalizeValue(row.Account),
                        Amount = resolvedAmount,
                        Discount = resolvedDiscount,
                        TotalAmountDue = resolvedTotalAmountDue,
                        AmountPaid = resolvedAmountPaid,
                    });
                }

                if (itemsToCreate.Count > 0)
                {
                    dbContext.PatientProgressNotes.AddRange(itemsToCreate);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                result.ImportedCount = itemsToCreate.Count;
                result.SkippedCount = skippedCount;
                result.Errors = errors;
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

        private static List<PatientLookupItem> ResolvePatientMatches(
            Dictionary<string, List<PatientLookupItem>> patientLookup,
            Dictionary<string, List<PatientLookupItem>> patientExactLookup,
            string lastName,
            string firstName,
            string middleName)
        {
            if (!string.IsNullOrWhiteSpace(middleName))
            {
                var exactKey = BuildExactNameKey(lastName, firstName, middleName);
                if (!string.IsNullOrWhiteSpace(exactKey) &&
                    patientExactLookup.TryGetValue(exactKey, out var exactMatches) &&
                    exactMatches.Count > 0)
                {
                    return exactMatches;
                }
            }

            var shortKey = BuildShortNameKey(lastName, firstName);
            if (!string.IsNullOrWhiteSpace(shortKey) &&
                patientLookup.TryGetValue(shortKey, out var shortMatches) &&
                shortMatches.Count > 0)
            {
                return shortMatches;
            }

            return [];
        }

        private static UploadRowsBuildResult BuildUploadRowsFromExcel(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("ProgressNotes", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return UploadRowsBuildResult.Fail("The uploaded workbook does not contain any worksheets.");
            }

            var firstUsedRow = worksheet.FirstRowUsed();
            if (firstUsedRow == null)
            {
                return UploadRowsBuildResult.Fail("The uploaded worksheet is empty.");
            }

            var headerMap = BuildHeaderMap(firstUsedRow);
            if (!headerMap.ContainsKey("FirstName") || !headerMap.ContainsKey("LastName"))
            {
                return UploadRowsBuildResult.Fail("The worksheet must contain FirstName and LastName columns.");
            }

            var rows = worksheet.RowsUsed()
                .Skip(1)
                .Select(row => BuildUploadRow(row, headerMap))
                .Where(row => !IsEmptyRow(row))
                .ToList();

            return UploadRowsBuildResult.Ok(rows);
        }

        private static UploadRowsBuildResult BuildUploadRowsFromCsv(byte[] fileContent)
        {
            var csvText = Encoding.UTF8.GetString(fileContent);
            if (string.IsNullOrWhiteSpace(csvText))
            {
                return UploadRowsBuildResult.Fail("The uploaded csv file is empty.");
            }

            var rawLines = csvText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (rawLines.Count == 0)
            {
                return UploadRowsBuildResult.Fail("The uploaded csv file is empty.");
            }

            var headerValues = ParseCsvLine(rawLines[0]);
            var headerMap = BuildHeaderMap(headerValues);
            if (!headerMap.ContainsKey("FirstName") || !headerMap.ContainsKey("LastName"))
            {
                return UploadRowsBuildResult.Fail("The csv file must contain FirstName and LastName columns.");
            }

            var rows = new List<UploadRow>();
            for (var index = 1; index < rawLines.Count; index++)
            {
                var values = ParseCsvLine(rawLines[index]);
                var row = BuildUploadRow(values, headerMap, index + 1);
                if (!IsEmptyRow(row))
                {
                    rows.Add(row);
                }
            }

            return UploadRowsBuildResult.Ok(rows);
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                var headerName = cell.GetString().Trim();
                if (!string.IsNullOrWhiteSpace(headerName) &&
                    SupportedHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase))
                {
                    map[headerName] = cell.Address.ColumnNumber;
                }
            }

            return map;
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> headerValues)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < headerValues.Count; index++)
            {
                var headerName = headerValues[index].Trim();
                if (!string.IsNullOrWhiteSpace(headerName) &&
                    SupportedHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase))
                {
                    map[headerName] = index;
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
                AssignedDoctor = GetCellString(row, headerMap, "AssignedDoctor"),
                Date = GetCellString(row, headerMap, "Date"),
                NextVisit = GetCellString(row, headerMap, "NextVisit"),
                Procedure = GetCellString(row, headerMap, "Procedure"),
                Category = GetCellString(row, headerMap, "Category"),
                Remarks = GetCellString(row, headerMap, "Remarks"),
                ClinicalFinding = GetCellString(row, headerMap, "ClinicalFinding"),
                Assessment = GetCellString(row, headerMap, "Assessment"),
                ToothNumber = GetCellString(row, headerMap, "ToothNumber"),
                Balance = GetCellString(row, headerMap, "Balance"),
                Account = GetCellString(row, headerMap, "Account"),
                Amount = GetCellString(row, headerMap, "Amount"),
                Discount = GetCellString(row, headerMap, "Discount"),
                TotalAmountDue = GetCellString(row, headerMap, "TotalAmountDue"),
                AmountPaid = GetCellString(row, headerMap, "AmountPaid"),
            };
        }

        private static UploadRow BuildUploadRow(List<string> rowValues, Dictionary<string, int> headerMap, int rowNumber)
        {
            return new UploadRow
            {
                RowNumber = rowNumber,
                FirstName = GetCellString(rowValues, headerMap, "FirstName"),
                LastName = GetCellString(rowValues, headerMap, "LastName"),
                MiddleName = GetCellString(rowValues, headerMap, "MiddleName"),
                AssignedDoctor = GetCellString(rowValues, headerMap, "AssignedDoctor"),
                Date = GetCellString(rowValues, headerMap, "Date"),
                NextVisit = GetCellString(rowValues, headerMap, "NextVisit"),
                Procedure = GetCellString(rowValues, headerMap, "Procedure"),
                Category = GetCellString(rowValues, headerMap, "Category"),
                Remarks = GetCellString(rowValues, headerMap, "Remarks"),
                ClinicalFinding = GetCellString(rowValues, headerMap, "ClinicalFinding"),
                Assessment = GetCellString(rowValues, headerMap, "Assessment"),
                ToothNumber = GetCellString(rowValues, headerMap, "ToothNumber"),
                Balance = GetCellString(rowValues, headerMap, "Balance"),
                Account = GetCellString(rowValues, headerMap, "Account"),
                Amount = GetCellString(rowValues, headerMap, "Amount"),
                Discount = GetCellString(rowValues, headerMap, "Discount"),
                TotalAmountDue = GetCellString(rowValues, headerMap, "TotalAmountDue"),
                AmountPaid = GetCellString(rowValues, headerMap, "AmountPaid"),
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

        private static string GetCellString(List<string> rowValues, Dictionary<string, int> headerMap, string headerName)
        {
            if (!headerMap.TryGetValue(headerName, out var columnIndex) || columnIndex >= rowValues.Count)
            {
                return string.Empty;
            }

            return rowValues[columnIndex].Trim();
        }

        private static bool IsEmptyRow(UploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.FirstName)
                && string.IsNullOrWhiteSpace(row.LastName)
                && string.IsNullOrWhiteSpace(row.MiddleName)
                && string.IsNullOrWhiteSpace(row.AssignedDoctor)
                && string.IsNullOrWhiteSpace(row.Date)
                && string.IsNullOrWhiteSpace(row.NextVisit)
                && string.IsNullOrWhiteSpace(row.Procedure)
                && string.IsNullOrWhiteSpace(row.Category)
                && string.IsNullOrWhiteSpace(row.Remarks)
                && string.IsNullOrWhiteSpace(row.ClinicalFinding)
                && string.IsNullOrWhiteSpace(row.Assessment)
                && string.IsNullOrWhiteSpace(row.ToothNumber)
                && string.IsNullOrWhiteSpace(row.Balance)
                && string.IsNullOrWhiteSpace(row.Account)
                && string.IsNullOrWhiteSpace(row.Amount)
                && string.IsNullOrWhiteSpace(row.Discount)
                && string.IsNullOrWhiteSpace(row.TotalAmountDue)
                && string.IsNullOrWhiteSpace(row.AmountPaid);
        }

        private static bool TryParseDate(string rawValue, bool required, out DateTime? parsedDate, out string errorMessage)
        {
            parsedDate = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (required)
                {
                    errorMessage = "Date is required.";
                    return false;
                }

                return true;
            }

            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var standardDate))
            {
                parsedDate = standardDate;
                return true;
            }

            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate))
            {
                try
                {
                    parsedDate = DateTime.FromOADate(oaDate);
                    return true;
                }
                catch
                {
                }
            }

            errorMessage = required
                ? "Date must be a valid Excel date or YYYY-MM-DD string."
                : "NextVisit must be a valid Excel date or YYYY-MM-DD string.";
            return false;
        }

        private static bool TryParseOptionalWholeNumber(string rawValue, out int? parsedValue, out string errorMessage)
        {
            parsedValue = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
            {
                parsedValue = integerValue;
                return true;
            }

            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue) &&
                Math.Abs(numericValue % 1) < double.Epsilon)
            {
                parsedValue = Convert.ToInt32(numericValue);
                return true;
            }

            errorMessage = "ToothNumber must be a whole number.";
            return false;
        }

        private static bool TryParseOptionalNumber(string rawValue, out double? parsedValue, out string errorMessage)
        {
            parsedValue = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            var normalizedValue = rawValue.Replace(",", string.Empty).Trim();
            if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
            {
                parsedValue = numericValue;
                return true;
            }

            errorMessage = $"`{rawValue}` is not a valid number.";
            return false;
        }

        private static string NormalizeValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(" ",
                value.Trim()
                    .Replace(".", " ")
                    .Replace(",", " ")
                    .Replace("(", " ")
                    .Replace(")", " ")
                    .Replace("'", " ")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static string BuildShortNameKey(string? lastName, string? firstName)
        {
            return string.Join("|", NormalizeName(lastName), NormalizeName(firstName));
        }

        private static string BuildExactNameKey(string? lastName, string? firstName, string? middleName)
        {
            return string.Join("|", NormalizeName(lastName), NormalizeName(firstName), NormalizeName(middleName));
        }

        private static string FormatPatientLabel(string lastName, string firstName, string middleName)
        {
            var givenNames = string.Join(" ",
                new[] { firstName, middleName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()));

            if (!string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(givenNames))
            {
                return $"{lastName.Trim()}, {givenNames}";
            }

            return lastName.Trim();
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];

                if (character == '"')
                {
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (character == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(character);
            }

            values.Add(current.ToString());
            return values;
        }

        private class UploadRowsBuildResult
        {
            public bool Success { get; init; }
            public string ErrorMessage { get; init; } = string.Empty;
            public List<UploadRow> Rows { get; init; } = new();

            public static UploadRowsBuildResult Ok(List<UploadRow> rows) => new()
            {
                Success = true,
                Rows = rows,
            };

            public static UploadRowsBuildResult Fail(string errorMessage) => new()
            {
                Success = false,
                ErrorMessage = errorMessage,
            };
        }

        private class UploadRow
        {
            public int RowNumber { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
            public string AssignedDoctor { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string NextVisit { get; set; } = string.Empty;
            public string Procedure { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Remarks { get; set; } = string.Empty;
            public string ClinicalFinding { get; set; } = string.Empty;
            public string Assessment { get; set; } = string.Empty;
            public string ToothNumber { get; set; } = string.Empty;
            public string Balance { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Amount { get; set; } = string.Empty;
            public string Discount { get; set; } = string.Empty;
            public string TotalAmountDue { get; set; } = string.Empty;
            public string AmountPaid { get; set; } = string.Empty;
        }

        private class PatientLookupItem
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
        }
    }
}
