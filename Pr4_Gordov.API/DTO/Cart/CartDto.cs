namespace Pr4_Gordov.API.DTO.Cart;

public record AddToCartRequest(
    int ProductId,
    int Quantity
);

public record UpdateCartItemRequest(
    int Quantity
);

public record CartItemResponse(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal Price,
    string? ImagePath,
    int Quantity,
    int StockQuantity,
    decimal TotalPrice
);

public record CartResponse(
    List<CartItemResponse> Items,
    decimal TotalAmount
);