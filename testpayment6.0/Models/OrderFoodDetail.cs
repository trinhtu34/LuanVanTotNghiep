using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class OrderFoodDetail
{
    public int OrderFoodDetailsId { get; set; }

    public long OrderTableId { get; set; }

    public string DishId { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public string? Note { get; set; }

    public virtual Menu Dish { get; set; } = null!;

    public virtual OrderTable OrderTable { get; set; } = null!;
}
