namespace CallCenter.Shared.Entities;

public class CustomerUserTypePermission
{
    public int Id { get; set; }

    // Hangi kullanici tipine ait
    public int UserTypeId { get; set; }
    public CustomerUserType UserType { get; set; } = null!;

    // Yetki tipi (CustomerPermissionTypes ref)
    public int PermissionTypeId { get; set; }
}
