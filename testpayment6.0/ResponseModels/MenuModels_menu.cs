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

    // Model cho item trong giỏ hàng (session)
    public class CartItem
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Images { get; set; } = string.Empty;
        public string Descriptions { get; set; } = string.Empty;

        // Tính tổng tiền cho item này
        public decimal TotalPrice => Price * Quantity;
    }

    // Model cho giỏ hàng (session)
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Tính tổng số lượng món
        public int TotalQuantity => Items.Sum(x => x.Quantity);

        // Tính tổng tiền giỏ hàng
        public decimal TotalAmount => Items.Sum(x => x.TotalPrice);

        // Thêm món vào giỏ hàng
        public void AddItem(DishModels_Menu dish, int quantity = 1)
        {
            var existingItem = Items.FirstOrDefault(x => x.DishId == dish.DishId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    DishId = dish.DishId,
                    DishName = dish.DishName,
                    Price = dish.Price,
                    Quantity = quantity,
                    Images = dish.Images,
                    Descriptions = dish.Descriptions
                });
            }
        }

        // Cập nhật số lượng món
        public void UpdateQuantity(string dishId, int quantity)
        {
            var item = Items.FirstOrDefault(x => x.DishId == dishId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }

        // Xóa món khỏi giỏ hàng
        public void RemoveItem(string dishId)
        {
            var item = Items.FirstOrDefault(x => x.DishId == dishId);
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        // Xóa toàn bộ giỏ hàng  
        public void Clear()
        {
            Items.Clear();
        }
    }

    // ViewModel cho trang giỏ hàng
    public class CartViewModel
    {
        public ShoppingCart Cart { get; set; } = new ShoppingCart();
        public bool IsLoggedIn { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    // Model để truyền dữ liệu thanh toán
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<CartItem> OrderItems { get; set; } = new List<CartItem>();
    }
}