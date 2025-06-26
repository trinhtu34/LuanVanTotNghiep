namespace testpayment6._0.Areas.admin.Models
{
    public class TableAdmin
    {
        public int TableId { get; set; }

        public int? Capacity { get; set; }

        public decimal Deposit { get; set; }

        public string? Description { get; set; }

        public int RegionId { get; set; }
        public string? RegionName { get; set; } // Tên khu vực
    }
}
