using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Models
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(10000, 100000000, ErrorMessage = "Số tiền phải từ 10.000đ đến 100.000.000đ")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        [Display(Name = "Mô tả giao dịch")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Mã đơn hàng")]
        public long OrderTableId { get; set; }
    }

    public class PaymentResultViewModel
    {
        public long? OrderTableId { get; set; }
        public long? CartId { get; set; }
        public decimal Amount { get; set; }
        public long PaymentId { get; set; }
        public bool IsSuccess { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public long VnpayTransactionId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string BankTransactionId { get; set; } = string.Empty;
        public string ResponseDescription { get; set; } = string.Empty;
        public string TransactionStatusDescription { get; set; } = string.Empty;
    }
}

