using System;
using System.Collections.Generic;

namespace Pr4_Gordov.API.Models;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public int StatusId { get; set; }

    public int DeliveryTypeId { get; set; }

    public string? DeliveryAddress { get; set; }

    public int? PickupPointId { get; set; }

    public decimal TotalCost { get; set; }

    public virtual DeliveryType DeliveryType { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PickupPoint? PickupPoint { get; set; }

    public virtual OrderStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}