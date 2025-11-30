namespace Product.Domain.Entities;

public class ProductReview
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string? ReviewerName { get; private set; }
    public string? ReviewerAvatar { get; private set; }
    public Guid? OrderId { get; private set; }
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Content { get; private set; }
    public bool IsVerifiedPurchase { get; private set; } = false;
    public bool IsApproved { get; private set; } = false;
    public int HelpfulCount { get; private set; } = 0;
    public string[]? Images { get; private set; }
    public string? SellerReply { get; private set; }
    public DateTime? SellerReplyAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductEntity? Product { get; set; }

    private ProductReview() { }

    public ProductReview(Guid productId, Guid userId, int rating, string? reviewerName = null, string? reviewerAvatar = null, string? title = null, string? content = null, Guid? orderId = null, string[]? images = null)
    {
        ProductId = productId;
        UserId = userId;
        ReviewerName = reviewerName;
        ReviewerAvatar = reviewerAvatar;
        Rating = rating;
        Title = title;
        Content = content;
        OrderId = orderId;
        Images = images;
        IsVerifiedPurchase = orderId.HasValue;
    }

    public void UpdateReviewerInfo(string? reviewerName, string? reviewerAvatar)
    {
        ReviewerName = reviewerName;
        ReviewerAvatar = reviewerAvatar;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(int rating, string? title, string? content, string[]? images)
    {
        Rating = rating;
        Title = title;
        Content = content;
        Images = images;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve() { IsApproved = true; UpdatedAt = DateTime.UtcNow; }
    
    public void AddSellerReply(string reply)
    {
        SellerReply = reply;
        SellerReplyAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementHelpful() => HelpfulCount++;
}

