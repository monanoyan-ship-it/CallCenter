namespace CallCenter.Shared.Entities;

/// <summary>
/// Junction table: Personel - Organizasyon Birimi coka-cok iliskisi.
/// Bir personel birden fazla organizasyonda gorev alabilir (ornegin takim lideri 2 takimda).
/// </summary>
public class CustomerPersonnelOrganizationUnit
{
    public int Id { get; set; }

    public int PersonnelId { get; set; }
    public CustomerPersonnel Personnel { get; set; } = null!;

    public int OrganizationUnitId { get; set; }
    public CustomerOrganizationUnit OrganizationUnit { get; set; } = null!;
}
