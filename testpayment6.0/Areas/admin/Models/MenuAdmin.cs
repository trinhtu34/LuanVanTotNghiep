namespace testpayment6._0.Areas.admin.Models
{
    public class MenuAdmin
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int RegionId { get; set; }
        public string? Descriptions { get; set; }
        public string? Images { get; set; }

        public string? CategoryName { get; set; }
        public string? RegionName { get; set; }
    }
}