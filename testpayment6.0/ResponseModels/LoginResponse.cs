using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    public class LoginResponse
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("uPassword")]
        public string UPassword { get; set; }

        [JsonProperty("customerName")]
        public string CustomerName { get; set; }

        [JsonProperty("rolesId")]
        public int RolesId { get; set; }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("createAt")]
        public DateTime? CreateAt { get; set; }
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [Display(Name = "Tên đăng nhập")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string UPassword { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Display(Name = "Xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("UPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ tên")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = "";
    }

    public class ProfileViewModel
    {
        [Display(Name = "User ID")]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Mật khẩu phải có ít nhất 5 ký tự")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string UPassword { get; set; } = "";

        [Display(Name = "Tên khách hàng")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string PhoneNumber { get; set; } = "";

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }
    }

    public class UserProfileResponse
    {
        public string? UPassword { get; set; }
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
    public class MenuItem
    {
        public int Count { get; set; }
        public string DishId { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public string Descriptions { get; set; }
        public int CategoryId { get; set; }
        public int RegionId { get; set; }
        public string Images { get; set; }
    }
}
