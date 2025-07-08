using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Areas.admin.Models
{
    public class OrderTable_manage
    {
        public int OrderTableId { get; set; }
        public string UserId { get; set; }
        public DateTime StartingTime { get; set; }
        public bool IsCancel { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalDeposit { get; set; }
        public DateTime OrderDate { get; set; }
        public bool IsPaid { get; set; }

        // Navigation properties
        public List<OrderTableDetail_manage> OrderTableDetails { get; set; } = new List<OrderTableDetail_manage>();
        public List<OrderFoodDetail_manage> OrderFoodDetails { get; set; } = new List<OrderFoodDetail_manage>();
    }

    public class OrderTableDetail_manage
    {
        public int OrderTablesDetailsId { get; set; }
        public int OrderTableId { get; set; }
        public int TableId { get; set; }
    }

    public class OrderFoodDetail_manage
    {
        public int OrderFoodDetailsId { get; set; }
        public int OrderTableId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; }
    }

    public class OrderTableViewModel_manage
    {
        public List<OrderTable_manage> OrderTables { get; set; } = new List<OrderTable_manage>();
        public string FilterType { get; set; } = "current"; // "current" or "all"
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
