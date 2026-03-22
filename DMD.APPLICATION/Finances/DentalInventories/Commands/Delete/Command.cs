using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Finances.DentalInventories.Commands.Delete
{
    [JsonSchema("DeleteDentalInventoryCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
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
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    return new BadRequestResponse("Inventory ID is required.");
                }

                var inventoryId = await protectionProvider.DecryptIntIdAsync(
                    request.Id,
                    ProtectedIdPurpose.DentalInventory);

                var item = await dbContext.DentalInventories
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        x => x.Id == inventoryId && x.ClinicProfileId == clinicId,
                        cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Inventory record was not found.");
                }

                dbContext.DentalInventories.Remove(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<bool>(true);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
