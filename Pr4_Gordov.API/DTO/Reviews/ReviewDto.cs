namespace Pr4_Gordov.API.DTO.Reviews;

public record CreateReviewRequest(
    int ProductId,
    int Rating,
    string? Comment
);

public record ReviewResponse(
    int Id,
    int ProductId,
    int UserId,
    string UserLogin,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);