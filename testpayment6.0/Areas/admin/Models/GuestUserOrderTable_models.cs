namespace testpayment6._0.Areas.admin.Models
{
    public class AdminBookingRequest
    {
        public string UserId { get; set; }
        public string StartingTime { get; set; }
        public List<SelectedTableRequest> SelectedTables { get; set; }
        public List<SelectedFoodRequest> SelectedFoods { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalFoodPrice { get; set; }
        public decimal GrandTotal { get; set; }
        //public bool IsAdminBooking { get; set; }
        //public string BookingType { get; set; }
    }

    public class SelectedTableRequest
    {
        public string TableId { get; set; }
        public decimal Deposit { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
    }

    public class SelectedFoodRequest
    {
        public string DishId { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
