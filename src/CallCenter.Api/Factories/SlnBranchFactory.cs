using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnBranchFactory : ISlnBranchFactory
{
    private readonly ISlnBranchEntityService _branches;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ICustomerEntityService _customers;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnCashRegisterEntityService _cashRegisters;
    private readonly ISlnExpenseEntityService _expenses;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnBranchFactory> _logger;

    public SlnBranchFactory(
        ISlnBranchEntityService branches,
        ICustomerPersonnelEntityService personnel,
        ICustomerEntityService customers,
        ISlnAppointmentEntityService appointments,
        ISlnInvoiceEntityService invoices,
        ISlnCashRegisterEntityService cashRegisters,
        ISlnExpenseEntityService expenses,
        IHttpClientFactory httpFactory,
        IUnitOfWork uow,
        ILogger<SlnBranchFactory> logger)
    {
        _branches = branches;
        _personnel = personnel;
        _customers = customers;
        _appointments = appointments;
        _invoices = invoices;
        _cashRegisters = cashRegisters;
        _expenses = expenses;
        _httpFactory = httpFactory;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Nominatim (OSM) ile ucretsiz geocoding — adres → lat/lng. Null dönerse konum yok.</summary>
    private async Task<(double? Lat, double? Lng)> GeocodeAsync(string? address, string? district, string? city)
    {
        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(district) && string.IsNullOrWhiteSpace(address))
            return (null, null);
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address)) parts.Add(address);
        if (!string.IsNullOrWhiteSpace(district)) parts.Add(district!);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city!);
        parts.Add("Türkiye");
        var query = System.Net.WebUtility.UrlEncode(string.Join(", ", parts));

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(6);
            // Nominatim UA zorunlu (kullanim kosulu)
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CorpLynk-Salon/1.0 (support@corplynk.com)");
            var url = $"https://nominatim.openstreetmap.org/search?q={query}&format=json&limit=1&addressdetails=0";
            var json = await client.GetStringAsync(url);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return (null, null);
            var first = doc.RootElement[0];
            if (first.TryGetProperty("lat", out var latProp) && first.TryGetProperty("lon", out var lngProp))
            {
                if (double.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                 && double.TryParse(lngProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                    return (lat, lng);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding hatasi (address={Address})", string.Join(", ", parts));
        }
        return (null, null);
    }

    public async Task<List<SlnBranchDto>> GetBranchesAsync(int customerId)
    {
        // Sube yoksa otomatik merkez sube olustur
        var hasAny = await _branches.GetAllQueryable().AnyAsync(b => b.CustomerId == customerId);
        if (!hasAny)
        {
            var customer = await _customers.GetByIdAsync(customerId);
            var customerName = customer?.Name ?? "Merkez";

            var hq = new SlnBranch
            {
                CustomerId = customerId,
                Name = "Merkez",
                Slug = GenerateSlug(customerName),
                IsHeadquarter = true,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow,
                CompanyTitle = customerName
            };
            _branches.Add(hq);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Otomatik merkez sube olusturuldu: CustomerId={CustomerId}", customerId);
        }

        var branches = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderBy(b => b.Name)
            .ToListAsync();

        // Manager isimlerini cek
        var managerIds = branches
            .Where(b => b.ManagerPersonnelId.HasValue)
            .Select(b => b.ManagerPersonnelId!.Value)
            .Distinct()
            .ToList();

        var managerNames = new Dictionary<int, string>();
        if (managerIds.Count > 0)
        {
            managerNames = await _personnel.GetAllQueryable()
                .Where(p => managerIds.Contains(p.Id))
                .Include(p => p.User)
                .ToDictionaryAsync(p => p.Id, p => p.User?.FullName ?? "");
        }

        return branches.Select(b => MapToDto(b, b.ManagerPersonnelId.HasValue
            ? managerNames.GetValueOrDefault(b.ManagerPersonnelId.Value) : null)).ToList();
    }

    public async Task<SlnBranchDto?> GetBranchAsync(int branchId, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return null;

        string? managerName = null;
        if (branch.ManagerPersonnelId.HasValue)
        {
            var manager = await _personnel.GetAllQueryable()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == branch.ManagerPersonnelId.Value);
            managerName = manager?.User?.FullName;
        }

        return MapToDto(branch, managerName);
    }

    public async Task<SlnBranchDto> CreateBranchAsync(SlnBranchCreateDto dto, int customerId)
    {
        var branch = new SlnBranch
        {
            CustomerId = customerId,
            Name = dto.Name,
            Slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : GenerateSlug(dto.Name),
            Address = dto.Address,
            City = NormalizeTrCity(dto.City),
            District = NormalizeTrCity(dto.District),
            Phone = dto.Phone,
            Email = dto.Email,
            GoogleMapsUrl = dto.GoogleMapsUrl,
            // Default calisma saatleri: Pzt-Cmt 09:00-19:00, Pazar kapali (yoksa)
            WorkingHoursJson = string.IsNullOrWhiteSpace(dto.WorkingHoursJson) ? DefaultWorkingHoursJson : dto.WorkingHoursJson,
            ManagerPersonnelId = dto.ManagerPersonnelId,
            IsHeadquarter = dto.IsHeadquarter,
            IsActive = dto.IsActive,
            ActivatedAt = dto.IsActive ? DateTime.UtcNow : null,
            CompanyTitle = dto.CompanyTitle,
            TaxOffice = dto.TaxOffice,
            TaxNumber = dto.TaxNumber,
            MersisNo = dto.MersisNo
        };

        // PAY.7: Manuel koordinat yoksa geocode et (Nominatim, best-effort)
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            branch.Latitude = dto.Latitude;
            branch.Longitude = dto.Longitude;
        }
        else
        {
            var (lat, lng) = await GeocodeAsync(dto.Address, branch.District, branch.City);
            if (lat.HasValue && lng.HasValue)
            {
                branch.Latitude = lat;
                branch.Longitude = lng;
            }
        }

        _branches.Add(branch);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni sube olusturuldu: {BranchId} - {Name}", branch.Id, branch.Name);
        return MapToDto(branch, null);
    }

    public async Task<(bool Success, string? Error)> UpdateBranchAsync(int branchId, SlnBranchUpdateDto dto, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return (false, "Sube bulunamadi");

        branch.Name = dto.Name;
        branch.Slug = dto.Slug;
        branch.Address = dto.Address;
        branch.City = NormalizeTrCity(dto.City);
        branch.District = NormalizeTrCity(dto.District);
        branch.Phone = dto.Phone;
        branch.Email = dto.Email;
        branch.GoogleMapsUrl = dto.GoogleMapsUrl;
        branch.WorkingHoursJson = dto.WorkingHoursJson;
        branch.ManagerPersonnelId = dto.ManagerPersonnelId;
        branch.IsHeadquarter = dto.IsHeadquarter;
        // Aktif/pasif gecis tarihlerini takip et
        if (!dto.IsActive && branch.IsActive)
            branch.DeactivatedAt = DateTime.UtcNow;
        else if (dto.IsActive && !branch.IsActive)
        {
            branch.ActivatedAt = DateTime.UtcNow;
            branch.DeactivatedAt = null;
        }

        branch.IsActive = dto.IsActive;
        branch.CompanyTitle = dto.CompanyTitle;
        branch.TaxOffice = dto.TaxOffice;
        branch.TaxNumber = dto.TaxNumber;
        branch.MersisNo = dto.MersisNo;

        // PAY.7: Manuel koordinat gelirse kullan, yoksa (ve mevcut null ise) geocode et
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            branch.Latitude = dto.Latitude;
            branch.Longitude = dto.Longitude;
        }
        else if (!branch.Latitude.HasValue || !branch.Longitude.HasValue)
        {
            var (lat, lng) = await GeocodeAsync(dto.Address, branch.District, branch.City);
            if (lat.HasValue && lng.HasValue)
            {
                branch.Latitude = lat;
                branch.Longitude = lng;
            }
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnBranchDto MapToDto(SlnBranch b, string? managerName) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Slug = b.Slug,
        Address = b.Address,
        City = b.City,
        District = b.District,
        Phone = b.Phone,
        Email = b.Email,
        GoogleMapsUrl = b.GoogleMapsUrl,
        WorkingHoursJson = b.WorkingHoursJson,
        PhotoUrl = b.PhotoUrl,
        Latitude = b.Latitude,
        Longitude = b.Longitude,
        ManagerPersonnelId = b.ManagerPersonnelId,
        ManagerName = managerName,
        IsHeadquarter = b.IsHeadquarter,
        IsActive = b.IsActive,
        CompanyTitle = b.CompanyTitle,
        TaxOffice = b.TaxOffice,
        TaxNumber = b.TaxNumber,
        MersisNo = b.MersisNo,
        ActivatedAt = b.ActivatedAt,
        DeactivatedAt = b.DeactivatedAt,
        CreatedAt = b.CreatedAt
    };

    public async Task<(bool Success, string? Error)> DeleteBranchAsync(int branchId, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return (false, "Sube bulunamadi");
        if (branch.IsHeadquarter) return (false, "Merkez sube silinemez. Yalnizca pasif yapilabilir.");

        // Altinda kayit var mi kontrol et
        var hasRecords = await _appointments.GetAllQueryable().AnyAsync(a => a.BranchId == branchId)
            || await _invoices.GetAllQueryable().AnyAsync(i => i.BranchId == branchId)
            || await _cashRegisters.GetAllQueryable().AnyAsync(c => c.BranchId == branchId)
            || await _expenses.GetAllQueryable().AnyAsync(e => e.BranchId == branchId)
            || await _personnel.GetAllQueryable().AnyAsync(p => p.BranchId == branchId);

        if (hasRecords) return (false, "Bu subeye ait kayitlar var. Sube silinemez, yalnizca pasif yapilabilir.");

        _branches.Remove(branch);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Sube silindi: {BranchId}", branchId);
        return (true, null);
    }

    /// <summary>Default sube calisma saatleri: Pzt-Cmt 09:00-19:00, Pazar kapali</summary>
    private const string DefaultWorkingHoursJson =
        "{\"mon\":\"09:00-19:00\",\"tue\":\"09:00-19:00\",\"wed\":\"09:00-19:00\",\"thu\":\"09:00-19:00\",\"fri\":\"09:00-19:00\",\"sat\":\"09:00-19:00\",\"sun\":\"closed\"}";

    /// <summary>Eski subelerin WorkingHoursJson null olanlari default ile doldur</summary>
    public async Task<object> NormalizeWorkingHoursAsync(int customerId)
    {
        var orphan = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.WorkingHoursJson == null)
            .ToListAsync();
        foreach (var b in orphan) b.WorkingHoursJson = DefaultWorkingHoursJson;
        if (orphan.Count > 0) await _uow.SaveChangesAsync();
        return new { updated = orphan.Count };
    }

    public async Task<object> NormalizeAddressesAsync(int customerId)
    {
        var branches = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .ToListAsync();
        int updated = 0;
        foreach (var b in branches)
        {
            var newCity = NormalizeTrCity(b.City);
            var newDist = NormalizeTrCity(b.District);
            if (newCity != b.City || newDist != b.District)
            {
                b.City = newCity;
                b.District = newDist;
                updated++;
            }
        }
        if (updated > 0) await _uow.SaveChangesAsync();
        return new { total = branches.Count, updated };
    }

    // TR-aware: "istanbul  " -> "İstanbul", "KAHRAMANMARAŞ" -> "Kahramanmaraş"
    private static string? NormalizeTrCity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        var tr = new System.Globalization.CultureInfo("tr-TR");
        var s = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), "\\s+", " ");
        if (s.Length == 0) return s;
        var lower = s.ToLower(tr);
        var parts = lower.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                parts[i] = parts[i].Substring(0, 1).ToUpper(tr) + parts[i].Substring(1);
        }
        return string.Join(' ', parts);
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var slug = input.ToLowerInvariant().Trim();
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug;
    }
}
