namespace Product.Application.DTOs;

// Requests
public record CreateReviewRequest(
    int Rating,
    string? ReviewerName = null,        // Denormalized reviewer info
    string? ReviewerAvatar = null,
    string? Title = null,
    string? Content = null,
    string[]? Images = null
);

public record UpdateReviewRequest(
    int Rating,
    string? Title,
    string? Content,
    string[]? Images
);

public record ReplyReviewRequest(
    string Reply
);

// Responses
public record ReviewResponse(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    string? ReviewerName,
    string? ReviewerAvatar,
    int Rating,
    string? Title,
    string? Content,
    string[]? Images,
    bool IsVerifiedPurchase,
    int HelpfulCount,
    string? SellerReply,
    DateTime? SellerReplyAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ReviewSummaryResponse(
    decimal Average,
    int Count,
    Dictionary<int, int> Distribution // Key: rating (1-5), Value: count
);

