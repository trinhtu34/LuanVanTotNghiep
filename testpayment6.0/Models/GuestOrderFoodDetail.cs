using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class GuestOrderFoodDetail
{
    public int GuestOrderFoodDetailsId { get; set; }

    public int? GuestOrderFoodId { get; set; }

    public string DishId { get; set; } = null!;

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Menu Dish { get; set; } = null!;

    public virtual GuestOrderFood? GuestOrderFood { get; set; }
}
