namespace Shared.Messaging.Events;

/// <summary>
/// Event published when a user updates their profile
/// Consumed by Product/Order services to update denormalized data
/// </summary>
public record UserProfileUpdatedEvent
{
    public Guid UserId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

