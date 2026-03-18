using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Commands.AcceptDataPrivacy
{
    public class Command : IRequest<Response>
    {
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
                if (!int.TryParse(clinicIdValue, out var clinicId))
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(item => item.Id == clinicId, cancellationToken);

                if (clinic == null)
                {
                    return new NotFoundResponse("Clinic profile was not found.");
                }

                clinic.IsDataPrivacyAccepted = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<DataPrivacyStatusModel>(new DataPrivacyStatusModel
                {
                    ClinicId = clinic.Id,
                    ClinicName = clinic.ClinicName,
                    IsDataPrivacyAccepted = clinic.IsDataPrivacyAccepted,
                    IsLocked = clinic.IsLocked
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
