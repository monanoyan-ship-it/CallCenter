using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// PRIVACY NOTICE (AYDINLATMA METNİ) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class PrivacyNoticeDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? GreetingMessageId { get; set; }
    public string? GreetingMessageName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PrivacyNoticeListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? GreetingMessageName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PrivacyNoticeCreateDto
{
    [Required(ErrorMessage = "Musteri secimi zorunludur.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Metin tipi zorunludur.")]
    public int TypeId { get; set; }

    [Required(ErrorMessage = "Baslik zorunludur.")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Icerik zorunludur.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Versiyon zorunludur.")]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    public int? GreetingMessageId { get; set; }
    public DateTime? EffectiveFrom { get; set; }

    [MaxLength(200)]
    public string? ApprovedBy { get; set; }
}

public class PrivacyNoticeUpdateDto
{
    [MaxLength(300)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public int? GreetingMessageId { get; set; }

    [MaxLength(200)]
    public string? ApprovedBy { get; set; }
}

public class CallConsentDto
{
    [Required(ErrorMessage = "Arama kaydi zorunludur.")]
    public int CallRecordId { get; set; }

    [Required(ErrorMessage = "Musteri zorunludur.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Kisi tanimlayicisi zorunludur.")]
    [MaxLength(200)]
    public string SubjectIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kisi adi zorunludur.")]
    [MaxLength(200)]
    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Riza tipi zorunludur.")]
    public int ConsentTypeId { get; set; }

    [Required(ErrorMessage = "Riza durumu zorunludur.")]
    public int ConsentStatusId { get; set; }

    [MaxLength(50)]
    public string ConsentMethod { get; set; } = "Verbal";

    public int? PrivacyNoticeId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// CONSENT (RIZA) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class ConsentRecordDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ConsentTypeId { get; set; }
    public string ConsentTypeName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string SubjectIdentifier { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ConsentMethod { get; set; } = string.Empty;
    public string? LegalBasis { get; set; }
    public string? PrivacyNoticeVersion { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConsentCreateDto
{
    [Required(ErrorMessage = "Musteri secimi zorunludur.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Riza tipi zorunludur.")]
    public int ConsentTypeId { get; set; }

    public int StatusId { get; set; }

    [Required(ErrorMessage = "Kisi tanimlayicisi zorunludur.")]
    [MaxLength(200)]
    public string SubjectIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kisi adi zorunludur.")]
    [MaxLength(200)]
    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Riza yontemi zorunludur.")]
    [MaxLength(50)]
    public string ConsentMethod { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LegalBasis { get; set; }

    [MaxLength(50)]
    public string? PrivacyNoticeVersion { get; set; }

    public DateTime? ConsentDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class ConsentRevokeDto
{
    [Required(ErrorMessage = "Iptal eden kisi zorunludur.")]
    [MaxLength(200)]
    public string RevokedBy { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
// DATA SUBJECT REQUEST (İLGİLİ KİŞİ BAŞVURUSU) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class DataSubjectRequestDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int RequestTypeId { get; set; }
    public string RequestTypeName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterIdentifier { get; set; } = string.Empty;
    public string RequesterContact { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public string? ResponseDescription { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue => StatusId != 3 && StatusId != 4 && DateTime.UtcNow > Deadline;
}

public class DataSubjectRequestCreateDto
{
    [Required(ErrorMessage = "Musteri secimi zorunludur.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Basvuru tipi zorunludur.")]
    public int RequestTypeId { get; set; }

    [Required(ErrorMessage = "Basvuran adi zorunludur.")]
    [MaxLength(200)]
    public string RequesterName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Basvuran tanimlayicisi zorunludur.")]
    [MaxLength(200)]
    public string RequesterIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Iletisim bilgisi zorunludur.")]
    [MaxLength(500)]
    public string RequesterContact { get; set; } = string.Empty;

    [Required(ErrorMessage = "Basvuru aciklamasi zorunludur.")]
    [MaxLength(2000)]
    public string RequestDescription { get; set; } = string.Empty;
}

public class PublicDataSubjectRequestCreateDto
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [MaxLength(200)]
    public string RequesterName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi girin.")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Talep tipi zorunludur.")]
    public int RequestTypeId { get; set; }

    [Required(ErrorMessage = "Aciklama zorunludur.")]
    [MaxLength(2000)]
    public string RequestDescription { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Source { get; set; }

    [MaxLength(200)]
    public string? SalonSlug { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }
}

public class PublicDataSubjectRequestResultDto
{
    public Guid Uid { get; set; }
    public DateTime Deadline { get; set; }
}

public class DataSubjectRequestUpdateDto
{
    public int? StatusId { get; set; }

    [MaxLength(2000)]
    public string? ResponseDescription { get; set; }

    [MaxLength(200)]
    public string? AssignedToUserName { get; set; }

    public int? AssignedToUserId { get; set; }

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// DATA BREACH (VERİ İHLALİ) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class DataBreachDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int SeverityId { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime NotificationDeadline { get; set; }
    public DateTime? AuthorityNotifiedAt { get; set; }
    public DateTime? SubjectsNotifiedAt { get; set; }
    public int? AffectedSubjectCount { get; set; }
    public string? AffectedDataCategories { get; set; }
    public string? CauseSummary { get; set; }
    public string? MeasuresTaken { get; set; }
    public string? ReportedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue => StatusId < 4 && DateTime.UtcNow > NotificationDeadline;
}

public class DataBreachCreateDto
{
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Ciddiyet seviyesi zorunludur.")]
    public int SeverityId { get; set; }

    [Required(ErrorMessage = "Baslik zorunludur.")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aciklama zorunludur.")]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    public int? AffectedSubjectCount { get; set; }

    [MaxLength(1000)]
    public string? AffectedDataCategories { get; set; }

    [MaxLength(2000)]
    public string? CauseSummary { get; set; }
}

public class DataBreachUpdateDto
{
    public int? StatusId { get; set; }
    public DateTime? AuthorityNotifiedAt { get; set; }
    public DateTime? SubjectsNotifiedAt { get; set; }
    public int? AffectedSubjectCount { get; set; }

    [MaxLength(1000)]
    public string? AffectedDataCategories { get; set; }

    [MaxLength(2000)]
    public string? CauseSummary { get; set; }

    [MaxLength(2000)]
    public string? MeasuresTaken { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// RETENTION POLICY (SAKLAMA POLİTİKASI) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class RetentionPolicyDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public string LegalBasis { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RetentionPolicyCreateDto
{
    [Required(ErrorMessage = "Musteri secimi zorunludur.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Kategori zorunludur.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Saklama suresi zorunludur.")]
    [Range(1, 36500, ErrorMessage = "Saklama suresi 1-36500 gun arasi olmalidir.")]
    public int RetentionDays { get; set; }

    [Required(ErrorMessage = "Yasal dayanak zorunludur.")]
    [MaxLength(500)]
    public string LegalBasis { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// DATA DESTRUCTION LOG (VERİ İMHA KAYDI) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class DataDestructionLogDto
{
    public long Id { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int MethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string DataCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DestroyedRecordCount { get; set; }
    public string? LegalBasis { get; set; }
    public string ApprovedByUserName { get; set; } = string.Empty;
    public DateTime DestroyedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DataDestructionCreateDto
{
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Imha yontemi zorunludur.")]
    public int MethodId { get; set; }

    [Required(ErrorMessage = "Veri kategorisi zorunludur.")]
    [MaxLength(200)]
    public string DataCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aciklama zorunludur.")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Imha edilen kayit sayisi en az 1 olmalidir.")]
    public int DestroyedRecordCount { get; set; }

    [MaxLength(500)]
    public string? LegalBasis { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// DATA INVENTORY (VERİ ENVANTERİ) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class DataInventoryItemDto
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string DataCategory { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string DataSubjectGroup { get; set; } = string.Empty;
    public string? RecipientGroup { get; set; }
    public string? TransferCountry { get; set; }
    public int RetentionDays { get; set; }
    public string SecurityMeasures { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? VerbisRegistrationNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DataInventoryCreateDto
{
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Veri kategorisi zorunludur.")]
    [MaxLength(200)]
    public string DataCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Isleme amaci zorunludur.")]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yasal dayanak zorunludur.")]
    [MaxLength(500)]
    public string LegalBasis { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ilgili kisi grubu zorunludur.")]
    [MaxLength(200)]
    public string DataSubjectGroup { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RecipientGroup { get; set; }

    [MaxLength(100)]
    public string? TransferCountry { get; set; }

    [Range(1, 36500, ErrorMessage = "Saklama suresi 1-36500 gun arasi olmalidir.")]
    public int RetentionDays { get; set; }

    [Required(ErrorMessage = "Guvenlik onlemleri zorunludur.")]
    [MaxLength(1000)]
    public string SecurityMeasures { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VerbisRegistrationNo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// CROSS-BORDER TRANSFER (YURT DIŞI AKTARIM) DTO'lari
// ═══════════════════════════════════════════════════════════════

public class CrossBorderTransferDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientCountry { get; set; } = string.Empty;
    public string DataCategories { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int SafeguardId { get; set; }
    public string SafeguardName { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrossBorderTransferCreateDto
{
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Alici adi zorunludur.")]
    [MaxLength(300)]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alici ulkesi zorunludur.")]
    [MaxLength(100)]
    public string RecipientCountry { get; set; } = string.Empty;

    [Required(ErrorMessage = "Veri kategorileri zorunludur.")]
    [MaxLength(1000)]
    public string DataCategories { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aktarim amaci zorunludur.")]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guvence tipi zorunludur.")]
    public int SafeguardId { get; set; }

    [Required(ErrorMessage = "Yasal dayanak zorunludur.")]
    [MaxLength(500)]
    public string LegalBasis { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class CrossBorderTransferUpdateDto
{
    public bool? IsActive { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// KVKK DASHBOARD DTO
// ═══════════════════════════════════════════════════════════════

public class KvkkDashboardDto
{
    public int PendingRequests { get; set; }
    public int OverdueRequests { get; set; }
    public int ActiveBreaches { get; set; }
    public int TotalConsents { get; set; }
    public int RevokedConsents { get; set; }
    public int RetentionPolicies { get; set; }
    public int DestructionCount { get; set; }
    public int InventoryItems { get; set; }
    public int ActivePrivacyNotices { get; set; }
    public int TotalPrivacyNotices { get; set; }
    public int ActiveTransfers { get; set; }
    public List<DataSubjectRequestDto> RecentRequests { get; set; } = new();
    public List<DataBreachDto> RecentBreaches { get; set; } = new();
}
