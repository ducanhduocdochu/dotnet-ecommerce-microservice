namespace User.Application.DTOs;

public record UserPreferenceResponse(
    Guid Id,
    Guid UserId,
    string Language,
    string Currency,
    string Timezone,
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    bool MarketingEmails,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

