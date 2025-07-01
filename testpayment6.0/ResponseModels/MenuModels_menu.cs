using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    // Model cho món ăn (giữ nguyên)
    public class DishModels_Menu
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Descriptions { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int RegionId { get; set; }
        public string Images { get; set; } = string.Empty;
    }

    // Model cho danh mục món ăn (giữ nguyên)
    public class CategoryModels_Menu
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    // Model cho khu vực (giữ nguyên)
    public class RegionModels_Menu
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }

    // ViewModel cho trang Menu (giữ nguyên)
    public class MenuViewModel_menu
    {
        public List<DishModels_Menu> Dishes { get; set; } = new List<DishModels_Menu>();
        public List<CategoryModels_Menu> Categories { get; set; } = new List<CategoryModels_Menu>();
        public List<RegionModels_Menu> Regions { get; set; } = new List<RegionModels_Menu>();

        // Filters
        public int? SelectedCategoryId { get; set; }
        public int? SelectedRegionId { get; set; }
        public string SearchText { get; set; } = string.Empty;
    }
}