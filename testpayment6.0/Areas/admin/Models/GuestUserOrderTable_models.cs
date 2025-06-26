using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Areas.admin.Models
{
    // Đảm bảo BookingViewModel có các properties với tên chính xác
    public class BookingViewModel
    {
        // Quan trọng: Tên properties phải khớp chính xác với name attributes trong form
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int SelectedTableId { get; set; }

        public List<TableModel>? Tables { get; set; }
        public List<DishModel>? Dishes { get; set; }
        public List<SelectedDish>? SelectedDishes { get; set; }
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


    // Các model khác cần có properties phù hợp
    public class TableModel
    {
        public int tableId { get; set; }
        public int capacity { get; set; }
        public string description { get; set; } = string.Empty;
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
        public decimal totalPrice { get; set; }
        public decimal totalDeposit { get; set; }
        public DateTime orderDate { get; set; }
    }
}