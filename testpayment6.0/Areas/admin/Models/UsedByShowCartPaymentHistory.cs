namespace testpayment6._0.Areas.admin.Models
{
    public class PaymentHistoryModel_Cart
    {
        public int PaymentResultId { get; set; }
        public int CartId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
        public string? PaymentMethod { get; set; }
        public string? BankCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? TransactionStatusDescription { get; set; }
    }

    public class PaymentHistoryViewModel_Cart
    {
        public List<PaymentHistoryModel_Cart> PaymentHistory { get; set; } = new List<PaymentHistoryModel_Cart>();
        public bool? FilterBySuccess { get; set; } // null = tất cả, true = thành công, false = thất bại
        public DateTime? FromDate { get; set; } // Thêm trường để lọc theo ngày bắt đầu
        public DateTime? ToDate { get; set; } // Thêm trường để lọc theo ngày kết thúc
    }
}
