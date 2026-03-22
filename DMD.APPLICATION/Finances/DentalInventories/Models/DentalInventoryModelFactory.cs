using DMD.APPLICATION.Common.ProtectedIds;
using DMD.DOMAIN.Entities.FInances;
using DMD.SERVICES.ProtectionProvider;

namespace DMD.APPLICATION.Finances.DentalInventories.Models
{
    public static class DentalInventoryModelFactory
    {
        public static async Task<DentalInventoryModel> CreateAsync(
            DentalInventory item,
            IProtectionProvider protectionProvider)
        {
            return new DentalInventoryModel
            {
                Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.DentalInventory) ?? string.Empty,
                ClinicProfileId = await protectionProvider.EncryptIntIdAsync(item.ClinicProfileId, ProtectedIdPurpose.Clinic) ?? string.Empty,
                ItemCode = item.ItemCode,
                Name = item.Name,
                Description = item.Description,
                Category = item.Category,
                Type = item.Type,
                QuantityOnHand = item.QuantityOnHand,
                MinimumStockLevel = item.MinimumStockLevel,
                MaximumStockLevel = item.MaximumStockLevel,
                UnitOfMeasure = item.UnitOfMeasure,
                UnitCost = item.UnitCost,
                SellingPrice = item.SellingPrice,
                TotalValue = item.TotalValue,
                SupplierName = item.SupplierName,
                SupplierContactNumber = item.SupplierContactNumber,
                SupplierEmail = item.SupplierEmail,
                BatchNumber = item.BatchNumber,
                ManufacturingDate = item.ManufacturingDate,
                ExpirationDate = item.ExpirationDate,
                LastRestockedDate = item.LastRestockedDate,
                LastUsedDate = item.LastUsedDate,
                UsageCount = item.UsageCount,
                IsLowStock = item.IsLowStock,
                IsExpired = item.IsExpired,
                IsActive = item.IsActive
            };
        }
    }
}
