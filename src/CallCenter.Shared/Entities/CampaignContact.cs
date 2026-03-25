using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

/// <summary>
/// Kampanyadaki bir kisi ve arama durumu.
/// </summary>
public class CampaignContact
{
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public CallCampaign? Campaign { get; set; }

    public int CrmContactId { get; set; }
    public CrmContact? CrmContact { get; set; }

    /// <summary>Atanan operator (CustomerPersonnel)</summary>
    public int? AssignedPersonnelId { get; set; }
    public CustomerPersonnel? AssignedPersonnel { get; set; }

    public int StatusId { get; set; } = CampaignContactStatuses.Ids.Pending;

    /// <summary>Arama deneme sayisi</summary>
    public int AttemptCount { get; set; }

    /// <summary>Son arama zamani</summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Tamamlanma zamani</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Sonuc notu</summary>
    public string? ResultNotes { get; set; }

    /// <summary>Baglanan CallRecord (varsa)</summary>
    public int? CallRecordId { get; set; }
    public CallRecord? CallRecord { get; set; }
}
