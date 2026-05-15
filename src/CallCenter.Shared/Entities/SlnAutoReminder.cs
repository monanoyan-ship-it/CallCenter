namespace CallCenter.Shared.Entities;

public class SlnAutoReminder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    /// <summary>1=Birthday, 2=Anniversary, 3=AppointmentReminder, 4=InactiveClient</summary>
    public int ReminderTypeId { get; set; }

    public string MessageTemplate { get; set; } = string.Empty;

    /// <summary>Randevu hatirlatmasi icin kac gun once gonderilecek</summary>
    public int DaysBefore { get; set; }

    /// <summary>InactiveClient tipi icin pasif gun esigi</summary>
    public int InactiveDaysThreshold { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
