using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;
using System.Security.Cryptography;
using System.Text.Json;
using Shared.Messaging.RabbitMQ;
using Shared.Messaging.Events;

namespace User.Application.Services;

public class UserService
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IUserAddressRepository _addressRepository;
    private readonly IUserPaymentMethodRepository _paymentMethodRepository;
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly IUserWishlistRepository _wishlistRepository;
    private readonly IUserActivityLogRepository _activityLogRepository;
    private readonly IEventPublisher? _eventPublisher;

    public UserService(
        IUserProfileRepository profileRepository,
        IUserAddressRepository addressRepository,
        IUserPaymentMethodRepository paymentMethodRepository,
        IUserPreferenceRepository preferenceRepository,
        IUserWishlistRepository wishlistRepository,
        IUserActivityLogRepository activityLogRepository,
        IEventPublisher? eventPublisher = null)
    {
        _profileRepository = profileRepository;
        _addressRepository = addressRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _preferenceRepository = preferenceRepository;
        _wishlistRepository = wishlistRepository;
        _activityLogRepository = activityLogRepository;
        _eventPublisher = eventPublisher;
    }

    // Profile Methods
    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        
        // Auto-create default profile if not exists (lazy creation)
        if (profile == null)
        {
            profile = await CreateDefaultProfileAsync(userId);
        }

        return new UserProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Phone,
            profile.DateOfBirth,
            profile.Gender,
            profile.AvatarUrl,
            profile.Bio,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    /// <summary>
    /// Create default profile for new user
    /// </summary>
    private async Task<UserProfile> CreateDefaultProfileAsync(Guid userId)
    {
        var profile = new UserProfile(userId);
        await _profileRepository.AddAsync(profile);
        await _profileRepository.SaveChangesAsync();
        return profile;
    }


    public async Task<UserProfileResponse> CreateOrUpdateProfileAsync(Guid userId, UpdateProfileRequest request, string? fullName = null)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        
        if (profile == null)
        {
            profile = new UserProfile(userId);
            profile.Update(request.Phone, request.DateOfBirth, request.Gender, request.AvatarUrl, request.Bio);
            await _profileRepository.AddAsync(profile);
        }
        else
        {
            profile.Update(request.Phone, request.DateOfBirth, request.Gender, request.AvatarUrl, request.Bio);
            await _profileRepository.UpdateAsync(profile);
        }

        await _profileRepository.SaveChangesAsync();

        // Publish event to sync denormalized data in other services
        PublishUserProfileUpdatedEvent(userId, fullName, null, request.Phone, request.AvatarUrl);

        return new UserProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Phone,
            profile.DateOfBirth,
            profile.Gender,
            profile.AvatarUrl,
            profile.Bio,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    /// <summary>
    /// Publish event when user profile is updated (for denormalized data sync)
    /// </summary>
    private void PublishUserProfileUpdatedEvent(Guid userId, string? fullName, string? email, string? phone, string? avatarUrl)
    {
        if (_eventPublisher == null) return;

        try
        {
            var evt = new UserProfileUpdatedEvent
            {
                UserId = userId,
                FullName = fullName,
                Email = email,
                Phone = phone,
                AvatarUrl = avatarUrl
            };

            _eventPublisher.Publish(
                EventConstants.UserExchange,
                EventConstants.UserProfileUpdated,
                evt
            );
        }
        catch
        {
            // Log but don't fail the request if event publishing fails
        }
    }

    public async Task<string?> UpdateAvatarAsync(Guid userId, string avatarUrl)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            profile = new UserProfile(userId);
            await _profileRepository.AddAsync(profile);
        }

        profile.UpdateAvatar(avatarUrl);
        await _profileRepository.UpdateAsync(profile);
        await _profileRepository.SaveChangesAsync();

        return profile.AvatarUrl;
    }

    // Address Methods
    public async Task<List<UserAddressResponse>> GetAddressesAsync(Guid userId)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        return addresses.Select(a => new UserAddressResponse(
            a.Id, a.UserId, a.FullName, a.Phone, a.AddressLine1, a.AddressLine2,
            a.City, a.StateProvince, a.PostalCode, a.Country, a.IsDefault,
            a.AddressType, a.CreatedAt, a.UpdatedAt
        )).ToList();
    }

    public async Task<UserAddressResponse?> GetAddressAsync(Guid userId, Guid addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId) return null;

        return new UserAddressResponse(
            address.Id, address.UserId, address.FullName, address.Phone,
            address.AddressLine1, address.AddressLine2, address.City,
            address.StateProvince, address.PostalCode, address.Country,
            address.IsDefault, address.AddressType, address.CreatedAt, address.UpdatedAt
        );
    }

    public async Task<UserAddressResponse> CreateAddressAsync(Guid userId, CreateAddressRequest request)
    {
        // If setting as default, remove default from other addresses
        if (request.IsDefault)
        {
            var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
            foreach (var addr in existingAddresses.Where(a => a.IsDefault))
            {
                addr.RemoveDefault();
                await _addressRepository.UpdateAsync(addr);
            }
        }

        var address = new UserAddress(
            userId, request.FullName, request.Phone, request.AddressLine1,
            request.AddressLine2, request.City, request.StateProvince,
            request.PostalCode, request.Country, request.IsDefault, request.AddressType
        );

        await _addressRepository.AddAsync(address);
        await _addressRepository.SaveChangesAsync();

        return new UserAddressResponse(
            address.Id, address.UserId, address.FullName, address.Phone,
            address.AddressLine1, address.AddressLine2, address.City,
            address.StateProvince, address.PostalCode, address.Country,
            address.IsDefault, address.AddressType, address.CreatedAt, address.UpdatedAt
        );
    }

    public async Task<UserAddressResponse?> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId) return null;

        // If setting as default, remove default from other addresses
        if (request.IsDefault && !address.IsDefault)
        {
            var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
            foreach (var addr in existingAddresses.Where(a => a.IsDefault && a.Id != addressId))
            {
                addr.RemoveDefault();
                await _addressRepository.UpdateAsync(addr);
            }
        }

        address.Update(
            request.FullName, request.Phone, request.AddressLine1,
            request.AddressLine2, request.City, request.StateProvince,
            request.PostalCode, request.Country, request.IsDefault, request.AddressType
        );

        await _addressRepository.UpdateAsync(address);
        await _addressRepository.SaveChangesAsync();

        return new UserAddressResponse(
            address.Id, address.UserId, address.FullName, address.Phone,
            address.AddressLine1, address.AddressLine2, address.City,
            address.StateProvince, address.PostalCode, address.Country,
            address.IsDefault, address.AddressType, address.CreatedAt, address.UpdatedAt
        );
    }

    public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId) return false;

        await _addressRepository.RemoveAsync(address);
        await _addressRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId) return false;

        // Remove default from other addresses
        var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
        foreach (var addr in existingAddresses.Where(a => a.IsDefault && a.Id != addressId))
        {
            addr.RemoveDefault();
            await _addressRepository.UpdateAsync(addr);
        }

        address.SetAsDefault();
        await _addressRepository.UpdateAsync(address);
        await _addressRepository.SaveChangesAsync();
        return true;
    }

    // Payment Method Methods
    public async Task<List<UserPaymentMethodResponse>> GetPaymentMethodsAsync(Guid userId)
    {
        var methods = await _paymentMethodRepository.GetByUserIdAsync(userId);
        return methods.Select(m => new UserPaymentMethodResponse(
            m.Id, m.UserId, m.PaymentType, m.Provider, m.CardLastFour,
            m.CardHolderName, m.ExpiryMonth, m.ExpiryYear, m.IsDefault,
            m.IsActive, m.BillingAddressId, m.CreatedAt, m.UpdatedAt
        )).ToList();
    }

    public async Task<UserPaymentMethodResponse?> GetPaymentMethodAsync(Guid userId, Guid paymentMethodId)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
        if (method == null || method.UserId != userId) return null;

        return new UserPaymentMethodResponse(
            method.Id, method.UserId, method.PaymentType, method.Provider,
            method.CardLastFour, method.CardHolderName, method.ExpiryMonth,
            method.ExpiryYear, method.IsDefault, method.IsActive,
            method.BillingAddressId, method.CreatedAt, method.UpdatedAt
        );
    }

    public async Task<UserPaymentMethodResponse> CreatePaymentMethodAsync(Guid userId, CreatePaymentMethodRequest request)
    {
        // Extract last 4 digits from card number
        string? cardLastFour = null;
        if (!string.IsNullOrEmpty(request.CardNumber) && request.CardNumber.Length >= 4)
        {
            cardLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4);
        }

        // If setting as default, remove default from other payment methods
        if (request.IsDefault)
        {
            var existingMethods = await _paymentMethodRepository.GetByUserIdAsync(userId);
            foreach (var method in existingMethods.Where(m => m.IsDefault))
            {
                method.RemoveDefault();
                await _paymentMethodRepository.UpdateAsync(method);
            }
        }

        var paymentMethod = new UserPaymentMethod(
            userId, request.PaymentType, request.Provider, cardLastFour,
            request.CardHolderName, request.ExpiryMonth, request.ExpiryYear,
            request.IsDefault, request.BillingAddressId
        );

        await _paymentMethodRepository.AddAsync(paymentMethod);
        await _paymentMethodRepository.SaveChangesAsync();

        return new UserPaymentMethodResponse(
            paymentMethod.Id, paymentMethod.UserId, paymentMethod.PaymentType,
            paymentMethod.Provider, paymentMethod.CardLastFour, paymentMethod.CardHolderName,
            paymentMethod.ExpiryMonth, paymentMethod.ExpiryYear, paymentMethod.IsDefault,
            paymentMethod.IsActive, paymentMethod.BillingAddressId,
            paymentMethod.CreatedAt, paymentMethod.UpdatedAt
        );
    }

    public async Task<UserPaymentMethodResponse?> UpdatePaymentMethodAsync(Guid userId, Guid paymentMethodId, UpdatePaymentMethodRequest request)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
        if (method == null || method.UserId != userId) return null;

        // If setting as default, remove default from other payment methods
        if (request.IsDefault && !method.IsDefault)
        {
            var existingMethods = await _paymentMethodRepository.GetByUserIdAsync(userId);
            foreach (var m in existingMethods.Where(m => m.IsDefault && m.Id != paymentMethodId))
            {
                m.RemoveDefault();
                await _paymentMethodRepository.UpdateAsync(m);
            }
        }

        method.Update(
            request.CardHolderName, request.ExpiryMonth, request.ExpiryYear,
            request.BillingAddressId, request.IsDefault, request.IsActive
        );

        await _paymentMethodRepository.UpdateAsync(method);
        await _paymentMethodRepository.SaveChangesAsync();

        return new UserPaymentMethodResponse(
            method.Id, method.UserId, method.PaymentType, method.Provider,
            method.CardLastFour, method.CardHolderName, method.ExpiryMonth,
            method.ExpiryYear, method.IsDefault, method.IsActive,
            method.BillingAddressId, method.CreatedAt, method.UpdatedAt
        );
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
        if (method == null || method.UserId != userId) return false;

        await _paymentMethodRepository.RemoveAsync(method);
        await _paymentMethodRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(Guid userId, Guid paymentMethodId)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
        if (method == null || method.UserId != userId) return false;

        // Remove default from other payment methods
        var existingMethods = await _paymentMethodRepository.GetByUserIdAsync(userId);
        foreach (var m in existingMethods.Where(m => m.IsDefault && m.Id != paymentMethodId))
        {
            m.RemoveDefault();
            await _paymentMethodRepository.UpdateAsync(m);
        }

        method.SetAsDefault();
        await _paymentMethodRepository.UpdateAsync(method);
        await _paymentMethodRepository.SaveChangesAsync();
        return true;
    }

    // Preference Methods
    public async Task<UserPreferenceResponse> GetPreferencesAsync(Guid userId)
    {
        var preference = await _preferenceRepository.GetByUserIdAsync(userId);
        
        // Auto-create default preferences if not exists (lazy creation)
        if (preference == null)
        {
            preference = new UserPreference(userId);
            await _preferenceRepository.AddAsync(preference);
            await _preferenceRepository.SaveChangesAsync();
        }

        return new UserPreferenceResponse(
            preference.Id, preference.UserId, preference.Language,
            preference.Currency, preference.Timezone, preference.EmailNotifications,
            preference.SmsNotifications, preference.PushNotifications,
            preference.MarketingEmails, preference.CreatedAt, preference.UpdatedAt
        );
    }

    public async Task<UserPreferenceResponse> CreateOrUpdatePreferencesAsync(Guid userId, UpdatePreferenceRequest request)
    {
        var preference = await _preferenceRepository.GetByUserIdAsync(userId);
        
        if (preference == null)
        {
            preference = new UserPreference(userId);
            preference.Update(
                request.Language, request.Currency, request.Timezone,
                request.EmailNotifications, request.SmsNotifications,
                request.PushNotifications, request.MarketingEmails
            );
            await _preferenceRepository.AddAsync(preference);
        }
        else
        {
            preference.Update(
                request.Language, request.Currency, request.Timezone,
                request.EmailNotifications, request.SmsNotifications,
                request.PushNotifications, request.MarketingEmails
            );
            await _preferenceRepository.UpdateAsync(preference);
        }

        await _preferenceRepository.SaveChangesAsync();

        return new UserPreferenceResponse(
            preference.Id, preference.UserId, preference.Language,
            preference.Currency, preference.Timezone, preference.EmailNotifications,
            preference.SmsNotifications, preference.PushNotifications,
            preference.MarketingEmails, preference.CreatedAt, preference.UpdatedAt
        );
    }

    // Wishlist Methods
    public async Task<PagedResponse<UserWishlistResponse>> GetWishlistAsync(Guid userId, int page, int pageSize)
    {
        var items = await _wishlistRepository.GetByUserIdAsync(userId, page, pageSize);
        var total = await _wishlistRepository.GetCountByUserIdAsync(userId);

        return new PagedResponse<UserWishlistResponse>(
            items.Select(w => new UserWishlistResponse(w.Id, w.UserId, w.ProductId, w.CreatedAt)).ToList(),
            total,
            page,
            pageSize
        );
    }

    public async Task<UserWishlistResponse?> AddToWishlistAsync(Guid userId, AddWishlistRequest request)
    {
        var exists = await _wishlistRepository.ExistsAsync(userId, request.ProductId);
        if (exists) return null; // Already in wishlist

        var wishlist = new UserWishlist(userId, request.ProductId);
        await _wishlistRepository.AddAsync(wishlist);
        await _wishlistRepository.SaveChangesAsync();

        return new UserWishlistResponse(wishlist.Id, wishlist.UserId, wishlist.ProductId, wishlist.CreatedAt);
    }

    public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId)
    {
        var wishlist = await _wishlistRepository.GetByUserIdAndProductIdAsync(userId, productId);
        if (wishlist == null) return false;

        await _wishlistRepository.RemoveAsync(wishlist);
        await _wishlistRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsInWishlistAsync(Guid userId, Guid productId)
    {
        return await _wishlistRepository.ExistsAsync(userId, productId);
    }

    // Activity Log Methods
    public async Task<PagedResponse<UserActivityLog>> GetActivityLogsAsync(Guid userId, int page, int pageSize, string? activityType = null)
    {
        var items = await _activityLogRepository.GetByUserIdAsync(userId, page, pageSize, activityType);
        var total = await _activityLogRepository.GetCountByUserIdAsync(userId, activityType);

        return new PagedResponse<UserActivityLog>(items, total, page, pageSize);
    }

    public async Task LogActivityAsync(Guid userId, string activityType, object? activityData = null, string? ipAddress = null, string? userAgent = null)
    {
        JsonDocument? jsonData = null;
        if (activityData != null)
        {
            jsonData = JsonDocument.Parse(JsonSerializer.Serialize(activityData));
        }

        var activityLog = new UserActivityLog(userId, activityType, jsonData, ipAddress, userAgent);
        await _activityLogRepository.AddAsync(activityLog);
        await _activityLogRepository.SaveChangesAsync();
    }
}

