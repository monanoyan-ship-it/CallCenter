namespace CallCenter.Shared.Entities;

public class SlnTreatmentRecord
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? SlnAppointmentId { get; set; }
    public SlnAppointment? SlnAppointment { get; set; }

    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int? ServiceSessionPlanId { get; set; }
    public SlnServiceSessionPlan? ServiceSessionPlan { get; set; }
    public ICollection<SlnServiceSessionRecord> ServiceSessionRecords { get; set; } = [];

    public DateTime TreatmentDate { get; set; } = DateTime.UtcNow;
    public string? SkinTypeSnapshot { get; set; }
    public string? AllergiesSnapshot { get; set; }
    public string? ContraindicationsSnapshot { get; set; }
    public string? SessionNotes { get; set; }
    public string? DeviceParameters { get; set; }
    public string? ProductNotes { get; set; }
    public string? AftercareNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByPersonnelId { get; set; }
    public CustomerPersonnel? CreatedByPersonnel { get; set; }
}
