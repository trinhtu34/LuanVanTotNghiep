using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace testpayment6._0.Areas.admin.Models
{
    public class RegionViewModel
    {
        public int regionId { get; set; }
        public string regionName { get; set; }
    }
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
    // Updated BookingViewModel
    public class BookingViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<int> SelectedTableIds { get; set; } = new List<int>();
        public List<RegionViewModel>? Regions { get; set; }
        public List<TableModel>? Tables { get; set; }
        public List<DishModel>? Dishes { get; set; }
        public List<SelectedDish>? SelectedDishes { get; set; }
        public DateTime startingTime { get; set; }
    }

    // Thêm model cho selected table
    public class SelectedTable
    {
        public int TableId { get; set; }
        public bool IsSelected { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class OrderTableDetailApiResponse
    {
        public int orderTablesDetailsId { get; set; }
        public int orderTableId { get; set; }
        public int tableId { get; set; }
    }
    public class TableModel
    {
        public int tableId { get; set; }
        public int capacity { get; set; }
        public string description { get; set; } = string.Empty;
    }
    public class SelectedDish
    {
        public string DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderFoodDetailRequest
    {
        public int OrderTableId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class DishModel
    {
        public string dishId { get; set; }
        public string dishName { get; set; } = string.Empty;
        public string descriptions { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string images { get; set; } = string.Empty;
    }

    // API Request Models
    public class UserSignupRequest
    {
        public string UserId { get; set; }
        public string UPassword { get; set; } = "";
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
    }
    public class userResponse
    {
        public string userId { get; set; }
        public string? customerName { get; set; }
        public int rolesId { get; set; }
        public string? phoneNumber { get; set; }
        public DateTime? createAt { get; set; }
    }

    public class OrderTableRequest
    {
        public string userId { get; set; }
        public DateTime startingTime { get; set; }
        public bool isCancel { get; set; } = false;
        public decimal totalPrice { get; set; } = 0;
        public DateTime orderDate { get; set; }

    }

    public class OrderTableDetailRequest
    {
        public int orderTableId { get; set; }
        public string TableId { get; set; }
    }


    public class OrderTableResponse
    {
        public int orderTableId { get; set; }
        public string userId { get; set; }
        public DateTime startingTime { get; set; }
        public bool isCancel { get; set; }
        public decimal? totalPrice { get; set; }
        public decimal? totalDeposit { get; set; }
        public DateTime orderDate { get; set; }
        public bool IsPaid { get; set; }
    }
}