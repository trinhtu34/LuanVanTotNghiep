namespace testpayment6._0.Areas.admin.Models
{
    public class PaymentHistoryModel_OrderTable
    {
        public int PaymentResultId { get; set; }
        public int OrderTableId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
        public string? PaymentMethod { get; set; }
        public string? BankCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? TransactionStatusDescription { get; set; }
    }

    public class PaymentHistoryViewModel_OrderTable
    {
        public List<PaymentHistoryModel_OrderTable> PaymentHistory { get; set; } = new List<PaymentHistoryModel_OrderTable>();
        public bool? FilterBySuccess { get; set; } // null = tất cả, true = thành công, false = thất bại
        public DateTime? FromDate { get; set; } // Thêm trường để lọc theo ngày bắt đầu
        public DateTime? ToDate { get; set; } // Thêm trường để lọc theo ngày kết thúc
    }
}
