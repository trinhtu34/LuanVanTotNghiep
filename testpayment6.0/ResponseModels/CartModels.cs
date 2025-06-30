// Models/CartModels.cs
using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    //public class CartItem
    //{
    //    public string DishId { get; set; } // Keep this definition
    //    public string DishName { get; set; }
    //    public decimal Price { get; set; }
    //    public int Quantity { get; set; }
    //    public string Image { get; set; }
    //    public decimal? Total => Quantity * Price;
    //}

    // Model cho API response
    public class CartResponseModel
    {
        public int CartId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderTime { get; set; }
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
    }

    public class AddCartDetailRequest
    {
        public string CartId { get; set; }
        public string DishId { get; set; }
        public string Quantity { get; set; }
        public string Price { get; set; }
    }
}