namespace testpayment6._0.Areas.admin.Models
{
    public class revenue_table
    {
        public int tableId { get; set; }
        public int capacity { get; set; }
        public string description { get; set; } = null;
        public int regionId { get; set; }
        public string regionName { get; set; } = null;
        public decimal totalRevenue { get; set; }
        public int orderCount { get; set; }
    }
    public class DishRevenueViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }
        public List<DishRevenueModel>? DishRevenue { get; set; } = new List<DishRevenueModel>();
        public List<CategoryModel>? Categories { get; set; } = new List<CategoryModel>();
    }
    public class DishRevenueModel
    {
        public string dishId { get; set; }
        public string dishName { get; set; }
        public string categoryName { get; set; }
        public decimal unitPrice { get; set; }
        public int totalQuantitySold { get; set; }
        public decimal totalRevenue { get; set; }
        public int orderCount { get; set; }
    }
    //public class DishRevenueViewModel
    //{
    //    public string dishId { get; set; }
    //    public string dishName { get; set; }
    //    public string categoryName { get; set; }
    //    public decimal unitPrice { get; set; }
    //    public int totalQuantitySold { get; set; }
    //    public decimal totalRevenue { get; set; }
    //    public int orderCount { get; set; }
    //}



    public class CategoryModel
    {
        public int categoryId { get; set; }
        public string categoryName { get; set; }
    }

    public class CategoryRevenueViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<CategoryRevenueModel> CategoryRevenue { get; set; } = new List<CategoryRevenueModel>();
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
    }

    public class CategoryRevenueModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int DishCount { get; set; }
        public int orderedDishCount { get; set; }
        public int OrderCount { get; set; }
    }
    public class RegionRevenueViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<RegionRevenueModel> RegionRevenue { get; set; } = new List<RegionRevenueModel>();
        public List<Region> regions { get; set; } = new List<Region>();
    }

    public class RegionRevenueModel
    {
        public int regionId { get; set; }
        public string regionName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int DishCount { get; set; }
        public int OrderCount { get; set; }
        public int orderTableCount { get; set; }
        public int cartCount { get; set; }
    }
    public class Region
    {
        public int regionId { get; set; }
        public string regionName { get; set; }
    }
}
