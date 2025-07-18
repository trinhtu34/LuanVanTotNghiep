using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Areas.admin.Models
{
    public class CartPaymentViewModel_adminPayment
    {
        [Required(ErrorMessage = "Cart ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Cart ID phải lớn hơn 0")]
        [Display(Name = "Cart ID")]
        public int CartId { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(1000, 50000000, ErrorMessage = "Số tiền phải từ 1,000 đến 50,000,000 VNĐ")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }
    }
}