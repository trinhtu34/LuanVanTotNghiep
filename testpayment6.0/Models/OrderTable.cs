using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class OrderTable
{
    public long OrderTableId { get; set; }

    public string? UserId { get; set; }

    public DateTime StartingTime { get; set; }

    public bool isCancel { get; set; }
    public decimal? TotalPrice { get; set; }

    public DateTime OrderDate { get; set; }

    public virtual ICollection<OrderFoodDetail> OrderFoodDetails { get; set; } = new List<OrderFoodDetail>();

    public virtual ICollection<OrderTablesDetail> OrderTablesDetails { get; set; } = new List<OrderTablesDetail>();

    public virtual ICollection<PaymentResult> PaymentResults { get; set; } = new List<PaymentResult>();

    public virtual User? User { get; set; }
}
