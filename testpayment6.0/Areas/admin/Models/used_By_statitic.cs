namespace testpayment6._0.Areas.admin.Models
{
    public class highest_revenue_table
    {
        public int tableId { get; set; }
        public int capacity { get; set; }
        public string description { get; set; } = null;
        public int regionId { get; set; }
        public string regionName { get; set; } = null;
        public decimal totalRevenue { get; set; }
        public int orderCount { get; set; }
    }
}
