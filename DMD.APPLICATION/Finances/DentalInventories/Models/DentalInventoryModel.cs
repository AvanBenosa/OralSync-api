using DMD.DOMAIN.Enums;

namespace DMD.APPLICATION.Finances.DentalInventories.Models
{
    public class DentalInventoryModel
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicProfileId { get; set; } = string.Empty;
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
        public bool IsLowStock { get; set; }
        public bool IsExpired { get; set; }
        public bool IsActive { get; set; }
    }
}
