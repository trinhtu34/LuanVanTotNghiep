using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Models
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(10000, 100000000, ErrorMessage = "Số tiền phải từ 10.000 đ đến 100.000.000 đ")]
        [Display(Name = "Số tiền thanh toán")]
        public double Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;
    }

    public class PaymentResultViewModel
    {
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