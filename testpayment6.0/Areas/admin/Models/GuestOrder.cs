namespace testpayment6._0.Areas.admin.Models
{
    // Model class dùng cho chức năng đặt bàn , được thao tác bởi nhân viên
    public class OrderViewModel
    {
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public List<RegionViewModel_Guest> Regions { get; set; } = new List<RegionViewModel_Guest>();
        public List<SelectedDish_Guest> SelectedDishes { get; set; } = new List<SelectedDish_Guest>();
    }

    public class RegionViewModel_Guest
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }

    public class MenuViewModel_Guest
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Descriptions { get; set; }
        public int CategoryId { get; set; }
        public int RegionId { get; set; }
        public string? Images { get; set; }
    }

    public class SelectedDish_Guest
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class CartResponse
    {
        public int CartId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderTime { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsCancel { get; set; }
    }
}