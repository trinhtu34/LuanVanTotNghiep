namespace testpayment6._0.ResponseModels
{
    public class CancelCartRequest
    {
        public long CartId { get; set; }
    }
    // Model cho response từ API thanh toán
    public class PaymentStatusApiResponse
    {
        public int OrderTableId { get; set; }
        public bool IsSuccess { get; set; }
    }

    // Có thể cập nhật PaymentStatusViewModel hiện tại nếu cần
    public class PaymentStatusViewModel_Order
    {
        public int CartId { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public decimal? Amount { get; set; }
    }

    // Model cho trang thanh toán
    public class PaymentRequestViewModel
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int CartId { get; set; }
        public int? OrderTableId { get; set; }
    }
}
