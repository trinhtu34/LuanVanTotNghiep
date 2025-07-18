using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    // Model cho form tạo liên hệ
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Nội dung liên hệ là bắt buộc")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung phải từ 10 đến 1000 ký tự")]
        [Display(Name = "Nội dung liên hệ")]
        public string Content { get; set; } = string.Empty;
    }

    // Model cho việc hiển thị danh sách liên hệ
    public class ContactListViewModel
    {
        public List<ContactItem> Contacts { get; set; } = new List<ContactItem>();
        public ContactFormViewModel NewContact { get; set; } = new ContactFormViewModel();
    }

    // Model cho từng item liên hệ
    public class ContactItem
    {
        public int ContactId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
    public class ContactResponse
    {
        public int ContactId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}