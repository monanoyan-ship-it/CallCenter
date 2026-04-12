using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PlatformAuthFactory : IPlatformAuthFactory
{
    private readonly IPlatformUserEntityService _userEs;
    private readonly TokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public PlatformAuthFactory(IPlatformUserEntityService userEs, TokenService tokenService, IUnitOfWork uow)
    {
        _userEs = userEs;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<(PlatformAuthResponse? Result, string? Error)> RegisterAsync(PlatformRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Password))
            return (null, "Telefon ve şifre zorunludur.");

        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        if (string.IsNullOrEmpty(normalizedPhone))
            return (null, "Geçerli bir telefon numarası giriniz.");

        var exists = await _userEs.GetAllQueryable().AnyAsync(u => u.Phone == normalizedPhone);
        if (exists)
            return (null, "Bu telefon numarası zaten kayıtlı.");

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var emailExists = await _userEs.GetAllQueryable().AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
                return (null, "Bu e-posta adresi zaten kayıtlı.");
        }

        var user = new PlatformUser
        {
            FullName = dto.FullName.Trim(),
            Phone = normalizedPhone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsPhoneVerified = false,
            IsEmailVerified = false
        };

        _userEs.Add(user);
        await _uow.SaveChangesAsync();

        var token = _tokenService.GeneratePlatformToken(user);
        return (new PlatformAuthResponse
        {
            Token = token,
            User = MapToDto(user)
        }, null);
    }

    public async Task<(PlatformAuthResponse? Result, string? Error)> LoginAsync(PlatformLoginDto dto)
    {
        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        var user = await _userEs.GetAllQueryable()
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Phone == normalizedPhone);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, "Telefon veya şifre hatalı.");

        if (!user.IsActive)
            return (null, "Hesabınız devre dışı bırakılmış.");

        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var token = _tokenService.GeneratePlatformToken(user);
        return (new PlatformAuthResponse
        {
            Token = token,
            User = MapToDto(user)
        }, null);
    }

    public async Task<PlatformUserDto?> GetMeAsync(int platformUserId)
    {
        var user = await _userEs.GetAllQueryable()
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Id == platformUserId);

        return user == null ? null : MapToDto(user);
    }

    public async Task<PlatformUserDto?> UpdateMeAsync(int platformUserId, PlatformUserUpdateDto dto)
    {
        var user = await _userEs.GetByIdAsync(platformUserId);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (dto.Email != null) user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<PlatformUserDto?> UpdateBillingInfoAsync(int platformUserId, PlatformBillingUpdateDto dto)
    {
        var user = await _userEs.GetByIdAsync(platformUserId);
        if (user == null) return null;

        user.BillingType = dto.BillingType;
        user.BillingFullName = dto.BillingFullName;
        user.BillingCompanyName = dto.BillingCompanyName;
        user.BillingTaxOffice = dto.BillingTaxOffice;
        user.BillingTaxNumber = dto.BillingTaxNumber;
        user.BillingAddress = dto.BillingAddress;
        user.BillingCity = dto.BillingCity;
        user.BillingDistrict = dto.BillingDistrict;
        user.BillingPostalCode = dto.BillingPostalCode;

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    private static PlatformUserDto MapToDto(PlatformUser u) => new()
    {
        Uid = u.Uid,
        FullName = u.FullName,
        Phone = u.Phone,
        Email = u.Email,
        AvatarUrl = u.AvatarUrl,
        SalonCount = u.Salons?.Count(s => s.IsActive) ?? 0,
        BillingType = u.BillingType,
        BillingFullName = u.BillingFullName,
        BillingCompanyName = u.BillingCompanyName,
        BillingTaxOffice = u.BillingTaxOffice,
        BillingTaxNumber = u.BillingTaxNumber,
        BillingAddress = u.BillingAddress,
        BillingCity = u.BillingCity,
        BillingDistrict = u.BillingDistrict,
        BillingPostalCode = u.BillingPostalCode
    };
}
