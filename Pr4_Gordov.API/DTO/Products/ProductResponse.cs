namespace Pr4_Gordov.API.DTO.Products;

public record ProductResponse(
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
);