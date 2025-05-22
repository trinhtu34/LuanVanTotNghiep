using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class Table
{
    public int TableId { get; set; }

    public int? Capacity { get; set; }

    public decimal? Deposit { get; set; }

    public string? Description { get; set; }

    public int RegionId { get; set; }

    public virtual ICollection<OrderTablesDetail> OrderTablesDetails { get; set; } = new List<OrderTablesDetail>();

    public virtual Region Region { get; set; } = null!;
}
