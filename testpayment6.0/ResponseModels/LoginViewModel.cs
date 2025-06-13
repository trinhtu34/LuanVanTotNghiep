// Models/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.ResponseModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string UPassword { get; set; }
    }
}

namespace testpayment6._0.ResponseModels
{
    public partial class TablesViewModel
    {
        public int TableId { get; set; }
        public int Capacity { get; set; }
        public decimal Deposit { get; set; }
        public string? Description { get; set; }
        public int RegionId { get; set; }

    }
}

namespace testpayment6._0.ResponseModels
{
    public class OrderTableResponse
    {
        public long OrderTableId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartingTime { get; set; }
        public bool IsCancel { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderTableViewModel
    {
        public long OrderTableId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartingTime { get; set; }
        public bool IsCancel { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
    }
    // Thêm ViewModel
    public class OrderTableDetailViewModel
    {
        public int OrderTablesDetailsId { get; set; }
        public long OrderTableId { get; set; }
        public int TableId { get; set; }
    }
}