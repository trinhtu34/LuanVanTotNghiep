using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class Menu
{
    public string DishId { get; set; } = null!;

    public string DishName { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Descriptions { get; set; }

    public int CategoryId { get; set; }

    public int RegionId { get; set; }

    public string? Images { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<GuestOrderFoodDetail> GuestOrderFoodDetails { get; set; } = new List<GuestOrderFoodDetail>();

    public virtual ICollection<OrderFoodDetail> OrderFoodDetails { get; set; } = new List<OrderFoodDetail>();

    public virtual Region Region { get; set; } = null!;
}
