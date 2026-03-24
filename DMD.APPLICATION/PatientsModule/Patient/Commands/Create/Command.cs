using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicProfileId { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public Suffix Suffix { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public BloodTypes BloodType { get; set; }
        public string CivilStatus { get; set; }
        public string ProfilePicture { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.mapper = mapper;
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                if (!int.TryParse(clinicIdValue, out var clinicId))
                {
                    return new BadRequestResponse("Clinic registration must be completed before creating patients.");
                }

                // 🔍 Check for duplicate (case-insensitive & trimmed)
                var isDuplicate = await dbContext.PatientInfos.AnyAsync(p =>
                    p.ClinicProfileId == clinicId &&
                    p.FirstName.Trim().ToLower() == request.FirstName.Trim().ToLower() &&
                    p.LastName.Trim().ToLower() == request.LastName.Trim().ToLower() &&
                    (p.MiddleName ?? "").Trim().ToLower() == (request.MiddleName ?? "").Trim().ToLower(),
                    cancellationToken);

                if (isDuplicate)
                {
                    return new BadRequestResponse("Patient already exists.");
                }

                var today = DateTime.Today;

                var countToday = await dbContext.PatientInfos
                    .CountAsync(p => p.CreatedAt.Date == today, cancellationToken);

                var sequence = countToday + 1;
                var patientNumber = $"DMD-{today:yyyyMMdd}-{sequence:D4}";

                var newItem = new PatientInfo
                {
                    ClinicProfileId = clinicId,
                    PatientNumber = patientNumber,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    MiddleName = request.MiddleName,
                    EmailAddress = request.EmailAddress,
                    Occupation = request.Occupation,
                    Religion = request.Religion,
                    Gender = request.Gender,
                    CivilStatus = request.CivilStatus,
                    Address = request.Address,
                    ContactNumber = request.ContactNumber,
                    BirthDate = request.BirthDate,
                    ProfilePicture = request.ProfilePicture,
                };

                dbContext.PatientInfos.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientModel>(newItem);
                response.Id = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient);
                response.ClinicProfileId = await protectionProvider.EncryptIntIdAsync(newItem.ClinicProfileId, ProtectedIdPurpose.Clinic);
                return new SuccessResponse<PatientModel>(response);
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
    }
}
