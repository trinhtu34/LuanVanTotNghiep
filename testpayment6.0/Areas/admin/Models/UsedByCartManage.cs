namespace testpayment6._0.Areas.admin.Models
{
    public class Cart_manage
    {
        public int CartId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderTime { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsCancel { get; set; }
        public bool IsPaid { get; set; }
        public bool IsFinish { get; set; }

        // Navigation properties
        public List<CartDetail_manage> CartDetails { get; set; } = new List<CartDetail_manage>();

        // Computed properties
        public string StatusDisplay =>
            IsCancel ? "Đã hủy" :
            IsFinish ? "Hoàn thành" :
            IsPaid ? "Đã thanh toán" :
            "Chưa thanh toán";

        public string StatusClass =>
            IsCancel ? "status-cancelled" :
            IsFinish ? "status-finished" :
            IsPaid ? "status-paid" :
            "status-unpaid";
    }

    public class CartDetail_manage
    {
        public int CartDetailsId { get; set; }
        public int CartId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        // Computed properties
        public decimal TotalPrice => Quantity * Price;

    }

    public class CartViewModel_manage
    {
        public string FilterType { get; set; } = "current";
        public List<Cart_manage> Carts { get; set; } = new List<Cart_manage>();
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }       // tổng tiền tất cả các đơn hàng kể cả đã hủy, đã thanh toán, chưa thanh toán
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int FinishedOrders { get; set; }
        public int PaidUnfinishedOrders { get; set; }     // Số đơn đã thanh toán chưa hoàn thành
        public int PaidFinishedOrders { get; set; }       // Số đơn đã thanh toán và hoàn thành
        public decimal UnpaidRevenue { get; set; }        // Tổng tiền chưa thanh toán
        public decimal PaidUnfinishedRevenue { get; set; } // Tổng tiền đã thanh toán chưa hoàn thành
        public decimal PaidFinishedRevenue { get; set; }   // Tổng tiền đã thanh toán và hoàn thành
    }


    public class CancelCartRequest
    {
        public int CartId { get; set; }
        public string Reason { get; set; }
    }


}
