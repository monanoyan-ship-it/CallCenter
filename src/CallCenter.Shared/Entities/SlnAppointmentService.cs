namespace CallCenter.Shared.Entities;

public class SlnAppointmentService
{
    public int Id { get; set; }
    public int SlnAppointmentId { get; set; }
    public SlnAppointment? SlnAppointment { get; set; }
    public int SlnServiceId { get; set; }
    public SlnService? SlnService { get; set; }
    public int SortOrder { get; set; }
}
