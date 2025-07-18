using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Areas.admin.Models
{
    public class OrdertablePaymentViewModel_adminPayment
    {
        [Required(ErrorMessage = "Mã đơn đặt bàn là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã đơn đặt bàn phải lớn hơn 0")]
        [Display(Name = "Mã đơn đặt bàn")]
        public int OrdertableId { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(1000, 50000000, ErrorMessage = "Số tiền phải từ 1,000 đến 50,000,000 VNĐ")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

    }
}
