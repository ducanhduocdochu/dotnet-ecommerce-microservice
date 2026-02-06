namespace User.Application.DTOs;

public record UpdatePreferenceRequest(
    string Language,
    string Currency,
    string Timezone,
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    bool MarketingEmails
);

