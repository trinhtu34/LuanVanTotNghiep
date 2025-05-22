using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class GuestUser
{
    public int GuestId { get; set; }

    public string? GuestName { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<GuestOrderFood> GuestOrderFoods { get; set; } = new List<GuestOrderFood>();
}
