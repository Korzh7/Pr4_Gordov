namespace Pr4_Gordov.API.DTO.Favorites;

public record AddFavoriteRequest(
    int ProductId
);

public record FavoriteItemResponse(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal Price,
    string? ImagePath,
    string CategoryName,
    bool IsAvailable,
    int StockQuantity
);