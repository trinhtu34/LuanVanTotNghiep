using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    public class TablesViewModel
    {
        [JsonPropertyName("tableId")]
        public int TableId { get; set; }

        [JsonPropertyName("regionId")]
        public int RegionId { get; set; }

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("deposit")]
        public decimal Deposit { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}

namespace testpayment6._0.ResponseModels
{
    public class OrderTableResponse
    {
        [JsonPropertyName("orderTableId")]
        public long OrderTableId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("startingTime")]
        public string StartingTime { get; set; } = string.Empty;

        [JsonPropertyName("isCancel")]
        public bool IsCancel { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }        
        [JsonPropertyName("totalDeposit")]
        public decimal? TotalDeposit { get; set; }

        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; } = string.Empty;
    }

    public class OrderTableViewModel
    {
        public long OrderTableId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartingTime { get; set; }
        public bool IsCancel { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? TotalDeposit { get; set; }
        public DateTime OrderDate { get; set; }
    }
    public class OrderTableDetailViewModel
    {
        [JsonPropertyName("orderTablesDetailsId")]
        public long OrderTablesDetailsId { get; set; }

        [JsonPropertyName("orderTableId")]
        public long OrderTableId { get; set; }

        [JsonPropertyName("tableId")]
        public int TableId { get; set; }
    }
    public class MenuViewModel
    {
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Descriptions { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int RegionId { get; set; }
        public string Images { get; set; } = string.Empty;
    }
    public class OrderFoodDetailViewModel
    {
        public long OrderFoodDetailId { get; set; }
        public long OrderTableId { get; set; }
        public string DishId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class OrderFoodRequest
    {
        public long OrderTableId { get; set; }
        public string DishId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? note { get; set; }
    }
}