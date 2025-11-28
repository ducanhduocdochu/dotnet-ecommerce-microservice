using System.Text.Json;

namespace User.Domain.Entities;

public class UserActivityLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string ActivityType { get; private set; } // LOGIN, VIEW_PRODUCT, ADD_TO_CART, etc.
    public JsonDocument? ActivityData { get; private set; } // JSON data
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public UserActivityLog(
        Guid userId,
        string activityType,
        JsonDocument? activityData,
        string? ipAddress,
        string? userAgent)
    {
        UserId = userId;
        ActivityType = activityType;
        ActivityData = activityData;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}

