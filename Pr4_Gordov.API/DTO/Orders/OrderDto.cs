namespace Pr4_Gordov.API.DTO.Orders;

public record CreateOrderRequest(
    int DeliveryTypeId,
    string? DeliveryAddress,
    int? PickupPointId,
    int? DirectProductId = null,
    int? DirectQuantity = null
);

public record OrderItemResponse(
    int Id,
    int ProductId,
    string ProductTitle,
    string? ImagePath,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record OrderResponse(
    int Id,
    DateTime OrderDate,
    string Status,
    string DeliveryType,
    string? DeliveryAddress,
    string? PickupPointName,
    string? PickupPointAddress,
    decimal TotalCost,
    List<OrderItemResponse> Items
);

public record PickupPointResponse(
    int Id,
    string Name,
    string Address,
    string WorkingHours
);