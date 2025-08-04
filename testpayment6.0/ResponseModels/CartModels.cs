// Models/CartModels.cs
using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    public class CartItem
    {
        public string DishId { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public decimal? Total => Quantity * Price;
    }
    public class GuestUser_orderfood
    {
        public string UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CustomerName { get; set; }
        public string address { get; set; }
        public string? email { get; set; }
    }

    // Model cho API response
    public class CartResponseModel
    {
        public int CartId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderTime { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsCancel { get; set; }
    }

    public class CartDetailResponseModel
    {
        public int CartDetailsId { get; set; }
        public int CartId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // Model cho API request
    public class CreateCartRequest
    {
        public string UserId { get; set; }
        public decimal TotalPrice { get; set; } // Thêm TotalPrice vào request
    }

    public class AddCartDetailRequest
    {
        public string CartId { get; set; }
        public string DishId { get; set; }
        public string Quantity { get; set; }
        public string Price { get; set; }
    }
}