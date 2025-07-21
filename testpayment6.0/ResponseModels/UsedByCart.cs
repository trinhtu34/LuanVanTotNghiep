using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    // Model cho Cart (đơn hàng)
    public class CartViewModel
    {
        public int CartId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderTime { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsCancel { get; set; }
        public bool IsPaid { get; set; } = false; // vừa thêm 07/19
        public bool? IsFinish { get; set; }  // vừa thêm 07/19, dùng tối ưu api thêm chút thôi , để đỡ phải query lại từ db , đỡ call api nhiều 
        public List<CartDetailViewModel> CartDetails { get; set; } = new List<CartDetailViewModel>();
    }

    // Model cho CartDetail (chi tiết đơn hàng)
    public class CartDetailViewModel
    {
        public int CartDetailsId { get; set; }
        public int CartId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // Model cho Payment Status
    public class PaymentStatusViewModel
    {
        public int CartId { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public decimal? Amount { get; set; }
    }

    // Model cho chi tiết đơn hàng
    public class OrderDetailViewModel
    {
        public CartViewModel Order { get; set; }
        public List<CartDetailViewModel> OrderDetails { get; set; } = new List<CartDetailViewModel>();
        public PaymentStatusViewModel PaymentStatus { get; set; }
    }
}