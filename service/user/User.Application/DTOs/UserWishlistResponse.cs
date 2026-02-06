namespace User.Application.DTOs;

public record UserWishlistResponse(
    Guid Id,
    Guid UserId,
    Guid ProductId,
    DateTime CreatedAt
);

