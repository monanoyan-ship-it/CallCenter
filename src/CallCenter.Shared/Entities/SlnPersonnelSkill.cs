namespace CallCenter.Shared.Entities;

/// <summary>Personelin yapabilecegi hizmetler</summary>
public class SlnPersonnelSkill
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }
}
