using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class Role
{
    public int RolesId { get; set; }

    public string? RolesDescription { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
