namespace User.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string? Phone { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public string? Gender { get; private set; } // MALE, FEMALE, OTHER
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public UserProfile(Guid userId)
    {
        UserId = userId;
    }

    public void Update(string? phone, DateTime? dateOfBirth, string? gender, string? avatarUrl, string? bio)
    {
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        AvatarUrl = avatarUrl;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}

