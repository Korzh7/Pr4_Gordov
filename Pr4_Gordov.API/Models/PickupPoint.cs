using System;
using System.Collections.Generic;

namespace Pr4_Gordov.API.Models;

public partial class PickupPoint
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string WorkingHours { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}