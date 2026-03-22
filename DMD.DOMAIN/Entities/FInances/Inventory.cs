using DMD.DOMAIN.Enums;

namespace DMD.DOMAIN.Entities.FInances
{
    public class DentalInventory : BaseEntity<int>
    {
        // Basic Information
        public int ClinicProfileId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Classification
        public InventoryCategory Category { get; set; }
        public InventoryType Type { get; set; }

        // Stock
        public decimal QuantityOnHand { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public decimal MaximumStockLevel { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;

        // Costing
        public decimal UnitCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalValue { get; set; }

        // Supplier
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierContactNumber { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;

        // Batch / Expiry
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        // Usage / Monitoring
        public DateTime? LastRestockedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public int UsageCount { get; set; }

        // Inventory Status
        public bool IsLowStock { get; set; }
        public bool IsExpired { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
