namespace Pr4_Gordov.WPF.Models;

public record AuthResponse(string Token, string Login, string Role);

public record ProductDto(
    int Id,
    string Title,
    string? Description,
    decimal Price,
    int CategoryId,
    string CategoryName,
    string? ImagePath,
    int? PackageQuantity,
    int StockQuantity,
    bool IsAvailable,
    double Rating
)
{
    public string PackageInfo => PackageQuantity.HasValue && PackageQuantity.Value > 1 ? $"({PackageQuantity.Value} шт/уп.)" : "";
}

public record UpsertProductDto(
    string Title,
    string? Description,
    decimal Price,
    int CategoryId,
    string? ImagePath,
    int? PackageQuantity,
    int StockQuantity,
    bool IsAvailable
);

public record CategoryDto(int Id, string Name);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal Price,
    string? ImagePath,
    int Quantity,
    int StockQuantity,
    decimal TotalPrice
);

public record CartDto(List<CartItemDto> Items, decimal TotalAmount);

public record FavoriteItemDto(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal Price,
    string? ImagePath,
    string CategoryName,
    bool IsAvailable,
    int StockQuantity
);

public record PickupPointDto(int Id, string Name, string Address, string WorkingHours);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductTitle,
    string? ImagePath,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record OrderDto(
    int Id,
    DateTime OrderDate,
    string Status,
    string DeliveryType,
    string? DeliveryAddress,
    string? PickupPointName,
    string? PickupPointAddress,
    decimal TotalCost,
    List<OrderItemDto> Items
);

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserLogin,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);