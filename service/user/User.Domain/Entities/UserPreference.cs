namespace User.Domain.Entities;

public class UserPreference
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Language { get; private set; } = "vi";
    public string Currency { get; private set; } = "VND";
    public string Timezone { get; private set; } = "Asia/Ho_Chi_Minh";
    public bool EmailNotifications { get; private set; } = true;
    public bool SmsNotifications { get; private set; } = false;
    public bool PushNotifications { get; private set; } = true;
    public bool MarketingEmails { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public UserPreference(Guid userId)
    {
        UserId = userId;
    }

    public void Update(
        string language,
        string currency,
        string timezone,
        bool emailNotifications,
        bool smsNotifications,
        bool pushNotifications,
        bool marketingEmails)
    {
        Language = language;
        Currency = currency;
        Timezone = timezone;
        EmailNotifications = emailNotifications;
        SmsNotifications = smsNotifications;
        PushNotifications = pushNotifications;
        MarketingEmails = marketingEmails;
        UpdatedAt = DateTime.UtcNow;
    }
}

