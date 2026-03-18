using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Commands.Update
{
    [JsonSchema("UpdateClinicProfileCommand")]
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public string OpeningTime { get; set; } = "09:00";
        public string ClosingTime { get; set; } = "18:00";
        public string LunchStartTime { get; set; } = "12:00";
        public string LunchEndTime { get; set; } = "13:00";
        public List<string> WorkingDays { get; set; } = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                var currentClinicId = int.TryParse(clinicIdValue, out var clinicId) ? clinicId : request.Id;

                var item = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == currentClinicId, cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                item.ClinicName = request.ClinicName.Trim();
                item.Address = request.Address.Trim();
                item.EmailAddress = request.EmailAddress.Trim();
                item.ContactNumber = request.ContactNumber.Trim();
                item.IsDataPrivacyAccepted = request.IsDataPrivacyAccepted;
                item.OpeningTime = request.OpeningTime.Trim();
                item.ClosingTime = request.ClosingTime.Trim();
                item.LunchStartTime = request.LunchStartTime.Trim();
                item.LunchEndTime = request.LunchEndTime.Trim();
                item.WorkingDays = request.WorkingDays ?? new List<string>();

                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<ClinicProfileModel>(new ClinicProfileModel
                {
                    Id = item.Id,
                    ClinicName = item.ClinicName,
                    Address = item.Address,
                    EmailAddress = item.EmailAddress,
                    ContactNumber = item.ContactNumber,
                    IsDataPrivacyAccepted = item.IsDataPrivacyAccepted,
                    OpeningTime = item.OpeningTime,
                    ClosingTime = item.ClosingTime,
                    LunchStartTime = item.LunchStartTime,
                    LunchEndTime = item.LunchEndTime,
                    WorkingDays = item.WorkingDays
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
