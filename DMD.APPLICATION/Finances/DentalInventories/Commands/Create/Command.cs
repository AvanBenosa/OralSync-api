using DMD.APPLICATION.Finances.DentalInventories.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.FInances;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Finances.DentalInventories.Commands.Create
{
    [JsonSchema("CreateDentalInventoryCommand")]
    public class Command : IRequest<Response>
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InventoryCategory Category { get; set; }
        public InventoryType Type { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public decimal MaximumStockLevel { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierContactNumber { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? LastRestockedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; } = true;
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

                var validationMessage = ValidateRequest(request);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return new BadRequestResponse(validationMessage);
                }

                var item = new DentalInventory
                {
                    ClinicProfileId = clinicId,
                    ItemCode = Normalize(request.ItemCode),
                    Name = Normalize(request.Name),
                    Description = Normalize(request.Description),
                    Category = request.Category,
                    Type = request.Type,
                    QuantityOnHand = request.QuantityOnHand,
                    MinimumStockLevel = request.MinimumStockLevel,
                    MaximumStockLevel = request.MaximumStockLevel,
                    UnitOfMeasure = Normalize(request.UnitOfMeasure),
                    UnitCost = request.UnitCost,
                    SellingPrice = request.SellingPrice,
                    TotalValue = request.TotalValue,
                    SupplierName = Normalize(request.SupplierName),
                    SupplierContactNumber = Normalize(request.SupplierContactNumber),
                    SupplierEmail = Normalize(request.SupplierEmail),
                    BatchNumber = Normalize(request.BatchNumber),
                    ManufacturingDate = request.ManufacturingDate?.Date,
                    ExpirationDate = request.ExpirationDate?.Date,
                    LastRestockedDate = request.LastRestockedDate?.Date,
                    LastUsedDate = request.LastUsedDate?.Date,
                    UsageCount = request.UsageCount,
                    IsActive = request.IsActive
                };

                RefreshDerivedStatuses(item);

                dbContext.DentalInventories.Add(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                var responseModel = await DentalInventoryModelFactory.CreateAsync(item, protectionProvider);
                return new SuccessResponse<DentalInventoryModel>(responseModel);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }

        private static string? ValidateRequest(Command request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Inventory name is required.";
            }

            if (string.IsNullOrWhiteSpace(request.UnitOfMeasure))
            {
                return "Unit of measure is required.";
            }

            if (!Enum.IsDefined(typeof(InventoryCategory), request.Category))
            {
                return "Inventory category is required.";
            }

            if (!Enum.IsDefined(typeof(InventoryType), request.Type))
            {
                return "Inventory type is required.";
            }

            if (request.QuantityOnHand < 0)
            {
                return "Quantity on hand cannot be negative.";
            }

            if (request.MinimumStockLevel < 0)
            {
                return "Minimum stock level cannot be negative.";
            }

            if (request.MaximumStockLevel < 0)
            {
                return "Maximum stock level cannot be negative.";
            }

            if (request.MaximumStockLevel < request.MinimumStockLevel)
            {
                return "Maximum stock level cannot be less than the minimum stock level.";
            }

            if (request.UnitCost < 0)
            {
                return "Unit cost cannot be negative.";
            }

            if (request.SellingPrice < 0)
            {
                return "Selling price cannot be negative.";
            }

            if (request.TotalValue < 0)
            {
                return "Total value cannot be negative.";
            }

            if (request.UsageCount < 0)
            {
                return "Usage count cannot be negative.";
            }

            if (!string.IsNullOrWhiteSpace(request.SupplierEmail))
            {
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(request.SupplierEmail))
                {
                    return "Supplier email must be a valid email address.";
                }
            }

            if (request.ManufacturingDate.HasValue &&
                request.ExpirationDate.HasValue &&
                request.ExpirationDate.Value.Date < request.ManufacturingDate.Value.Date)
            {
                return "Expiration date cannot be earlier than the manufacturing date.";
            }

            return null;
        }

        private static void RefreshDerivedStatuses(DentalInventory item)
        {
            item.IsLowStock = item.QuantityOnHand <= item.MinimumStockLevel;
            item.IsExpired = item.ExpirationDate.HasValue &&
                item.ExpirationDate.Value.Date < DateTime.UtcNow.Date;
        }

        private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
    }
}
