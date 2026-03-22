namespace DMD.APPLICATION.Finances.DentalInventories.Models
{
    public class DentalInventoryResponseModel
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public List<DentalInventoryModel> Items { get; set; } = new();
    }
}
