namespace User.Domain.Entities;

public class UserWishlist
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public UserWishlist(Guid userId, Guid productId)
    {
        UserId = userId;
        ProductId = productId;
    }
}

