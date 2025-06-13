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
    public class DatBanViewModel
    {
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int Capacity { get; set; }
        public decimal Deposit { get; set; }
        public string Description { get; set; }
        public int RegionId { get; set; }
        public string RegionName { get; set; }
        public DateTime StartingTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class OrderTableDetailViewModel
    {
        public int OrderTablesDetailsId { get; set; }
        public long OrderTableId { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int Capacity { get; set; }
        public decimal Deposit { get; set; }
    }

    public class CreateOrderRequest
    {
        public string UserId { get; set; }
        public DateTime StartingTime { get; set; }
        public bool IsCancel { get; set; } = false;
        public decimal TotalPrice { get; set; } = 0;
        public DateTime OrderDate { get; set; } = DateTime.Now;
    }

    public class CreateOrderDetailRequest
    {
        public string OrderTableId { get; set; }
        public string TableId { get; set; }
    }
}
namespace testpayment6._0.ResponseModels
{
    public class LoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string UPassword { get; set; } = string.Empty;
    }
}