namespace Pr4_Gordov.API.DTO.Admin;

public record UpsertProductRequest(
    string Title,
    string? Description,
    decimal Price,
    int CategoryId,
    string? ImagePath,
    int? PackageQuantity,
    int StockQuantity,
    bool IsAvailable
);

public record UpdateOrderStatusRequest(
    int StatusId
);

public record UpsertCategoryRequest(
    string Name
);

public record UpsertPickupPointRequest(
    string Name,
    string Address,
    string WorkingHours
);