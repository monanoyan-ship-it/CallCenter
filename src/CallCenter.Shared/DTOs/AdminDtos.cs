using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// ORTAK
// ═══════════════════════════════════════════════════════════════

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ═══════════════════════════════════════════════════════════════
// KULLANICI (USER)
// ═══════════════════════════════════════════════════════════════

public class UserListDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserCreateDto
{
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol secimi zorunludur.")]
    public int RoleId { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }
}

public class UserUpdateDto
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Rol secimi zorunludur.")]
    public int RoleId { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }

    public bool IsActive { get; set; } = true;
}

// ═══════════════════════════════════════════════════════════════
// MÜŞTERİ (CUSTOMER)
// ═══════════════════════════════════════════════════════════════

public class CustomerListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MaxUsers { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
    public int PersonnelCount { get; set; }
    public int QueueCount { get; set; }
    public int SipAccountCount { get; set; }
}

public class CustomerCreateDto
{
    [Required(ErrorMessage = "Firma adi zorunludur.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? TaxNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string? Email { get; set; }

    public int MaxUsers { get; set; }

    public decimal MonthlyUnitPrice { get; set; }

    [Required(ErrorMessage = "Admin kullanici adi zorunludur.")]
    [StringLength(50, MinimumLength = 3)]
    public string AdminUserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin sifresi zorunludur.")]
    [StringLength(100, MinimumLength = 8)]
    public string AdminPassword { get; set; } = string.Empty;
}

public class CustomerUpdateDto
{
    [Required(ErrorMessage = "Firma adi zorunludur.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? TaxNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public int MaxUsers { get; set; }

    public decimal MonthlyUnitPrice { get; set; }

    public bool SaveRecordingToPlatform { get; set; } = true;
    public bool SaveRecordingToOwnStorage { get; set; }
}

public class CustomerDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int MaxUsers { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
    public bool SaveRecordingToPlatform { get; set; }
    public bool SaveRecordingToOwnStorage { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PersonnelSimpleDto> Personnel { get; set; } = new();

    // Admin kullanici bilgileri
    public CustomerAdminInfoDto? AdminInfo { get; set; }
}

/// <summary>Musteri admin kullanicisinin ozet bilgileri</summary>
public class CustomerAdminInfoDto
{
    public int PersonnelId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Title { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class PersonnelSimpleDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
// KUYRUK (QUEUE)
// ═══════════════════════════════════════════════════════════════

public class QueueListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxWaitTimeSeconds { get; set; }
    public bool IsActive { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }
    public int AgentCount { get; set; }
}

public class QueueCreateDto
{
    [Required(ErrorMessage = "Kuyruk adi zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int MaxWaitTimeSeconds { get; set; } = 300;

    [Required(ErrorMessage = "Firma secimi zorunludur.")]
    public int CustomerId { get; set; }
}

public class QueueUpdateDto
{
    [Required(ErrorMessage = "Kuyruk adi zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int MaxWaitTimeSeconds { get; set; } = 300;

    public bool IsActive { get; set; } = true;
}

public class QueueDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxWaitTimeSeconds { get; set; }
    public bool IsActive { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<QueueAgentDto> Agents { get; set; } = new();
}

public class QueueAgentDto
{
    public int AgentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class QueueAgentAssignDto
{
    [Required]
    public int AgentId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SIP HESABI
// ═══════════════════════════════════════════════════════════════

public class SipAccountListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Transport { get; set; }
    public string? WsUri { get; set; }
    public bool UseSrtp { get; set; }
    public string? StunServer { get; set; }
    public string? TurnServer { get; set; }
    public string? PreferredCodecs { get; set; }
    public int JitterBufferMinMs { get; set; }
    public int JitterBufferMaxMs { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }
}

public class SipAccountCreateDto
{
    [Required(ErrorMessage = "Hesap adi zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sunucu adresi zorunludur.")]
    [StringLength(200)]
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 5060;

    [StringLength(200)]
    public string? Domain { get; set; }

    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [StringLength(256)]
    public string Password { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Transport { get; set; } = "UDP";

    [StringLength(500)]
    public string? WsUri { get; set; }

    public bool UseSrtp { get; set; }
    public bool IsDefault { get; set; }

    // TURN/ICE
    [StringLength(200)]
    public string? StunServer { get; set; }
    [StringLength(200)]
    public string? TurnServer { get; set; }
    [StringLength(100)]
    public string? TurnUsername { get; set; }
    [StringLength(256)]
    public string? TurnPassword { get; set; }

    // Codec tercihleri
    /// <summary>Codec oncelik sirasi, JSON array: ["opus","g722","pcmu","pcma"]. Bos ise varsayilan.</summary>
    public string? PreferredCodecs { get; set; }
    public int JitterBufferMinMs { get; set; }
    public int JitterBufferMaxMs { get; set; }

    [Required(ErrorMessage = "Firma secimi zorunludur.")]
    public int CustomerId { get; set; }
}

public class SipAccountUpdateDto
{
    [Required(ErrorMessage = "Hesap adi zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sunucu adresi zorunludur.")]
    [StringLength(200)]
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 5060;

    [StringLength(200)]
    public string? Domain { get; set; }

    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Password { get; set; }

    [StringLength(10)]
    public string? Transport { get; set; } = "UDP";

    [StringLength(500)]
    public string? WsUri { get; set; }

    public bool UseSrtp { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    // TURN/ICE
    [StringLength(200)]
    public string? StunServer { get; set; }
    [StringLength(200)]
    public string? TurnServer { get; set; }
    [StringLength(100)]
    public string? TurnUsername { get; set; }
    [StringLength(256)]
    public string? TurnPassword { get; set; }

    // Codec tercihleri
    public string? PreferredCodecs { get; set; }
    public int JitterBufferMinMs { get; set; }
    public int JitterBufferMaxMs { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// DİL YÖNETİMİ (TRANSLATION)
// ═══════════════════════════════════════════════════════════════

public class TranslationKeyListDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
}

public class TranslationKeyCreateDto
{
    [Required(ErrorMessage = "Key zorunludur.")]
    [StringLength(200, MinimumLength = 3)]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Modul zorunludur.")]
    [StringLength(50)]
    public string Module { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public Dictionary<string, string> Values { get; set; } = new();
}

public class TranslationKeyUpdateDto
{
    [StringLength(50)]
    public string? Module { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public Dictionary<string, string> Values { get; set; } = new();
}

public class LanguageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SİSTEM AYARLARI
// ═══════════════════════════════════════════════════════════════

public class SystemSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

public class SystemSettingUpdateDto
{
    [Required(ErrorMessage = "Deger zorunludur.")]
    public string Value { get; set; } = string.Empty;
}

public class SystemSettingCreateDto
{
    [Required(ErrorMessage = "Key zorunludur.")]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [Required(ErrorMessage = "Grup zorunludur.")]
    [StringLength(50)]
    public string Group { get; set; } = string.Empty;

    [StringLength(20)]
    public string ValueType { get; set; } = "string";

    [StringLength(500)]
    public string? Description { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// AUDIT LOG
// ═══════════════════════════════════════════════════════════════

public class AuditLogListDto
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
}

public class AuditLogDetailDto
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// CALL FORWARDING (ARAMA YÖNLENDİRME)
// ═══════════════════════════════════════════════════════════════

public class CallForwardingRuleDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int? CustomerId { get; set; }
    public int ForwardType { get; set; }
    public string ForwardTypeName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public int TimeoutSeconds { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CallForwardingRuleCreateDto
{
    [Required]
    public int UserId { get; set; }

    public int? CustomerId { get; set; }

    [Required]
    [Range(1, 4, ErrorMessage = "ForwardType 1-4 arasi olmali (Always, Busy, NoAnswer, Offline)")]
    public int ForwardType { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Destination { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public int TimeoutSeconds { get; set; } = 20;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class CallForwardingRuleUpdateDto
{
    [Range(1, 4, ErrorMessage = "ForwardType 1-4 arasi olmali")]
    public int? ForwardType { get; set; }

    [StringLength(200, MinimumLength = 1)]
    public string? Destination { get; set; }

    public bool? IsActive { get; set; }
    public int? Priority { get; set; }
    public int? TimeoutSeconds { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// FATURALAMA (BILLING)
// ═══════════════════════════════════════════════════════════════

public class BillingPeriodDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int UserCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

public class BillingPeriodUpdateDto
{
    public bool IsPaid { get; set; }
    public string? Notes { get; set; }
}

public class BulkBillingGenerateDto
{
    public int Year { get; set; }
    public int Month { get; set; }
}
