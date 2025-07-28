namespace testpayment6._0.Areas.admin.Models
{
    public class totalPaymentInfo
    {
        public long PaymentResultId { get; set; }

        public long? OrderTableId { get; set; }
        public long? CartId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }

        public string? Description { get; set; }

        public DateTime Timestamp { get; set; }

        public string? PaymentMethod { get; set; }
        public string? BankCode { get; set; }
        public string? ResponseDescription { get; set; }

        public string? TransactionStatusDescription { get; set; }
    }

    public class PaymentHistoryViewAll
    {
        public List<totalPaymentInfo> PaymentHistory { get; set; } = new List<totalPaymentInfo>();
        public bool? FilterBySuccess { get; set; } // null = tất cả, true = thành công, false = thất bại
        public DateTime? FromDate { get; set; } // Thêm trường để lọc theo ngày bắt đầu
        public DateTime? ToDate { get; set; } // Thêm trường để lọc theo ngày kết thúc
        public decimal? TotalAmount { get; set; } // Tổng số tiền thanh toán
    }
}
