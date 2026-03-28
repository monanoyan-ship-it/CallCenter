namespace CallCenter.Shared.Entities;

/// <summary>
/// Paket tanimi (orn: 10 seans sac bakimi, 5 seans cilt bakimi)
/// </summary>
public class SlnPackageDefinition
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int TotalSessions { get; set; }
    public decimal Price { get; set; }
    public int ValidDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Musteriye satilmis paket
/// </summary>
public class SlnClientPackage
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int PackageDefinitionId { get; set; }
    public SlnPackageDefinition? PackageDefinition { get; set; }

    public int? SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; }
    public int RemainingSessions { get; set; }
    public decimal PaidAmount { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public int? SoldByPersonnelId { get; set; }
    public CustomerPersonnel? SoldByPersonnel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnPackageUsage> Usages { get; set; } = [];
}

/// <summary>
/// Paket kullanim kaydi
/// </summary>
public class SlnPackageUsage
{
    public int Id { get; set; }
    public int ClientPackageId { get; set; }
    public SlnClientPackage? ClientPackage { get; set; }

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public string? Notes { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
