using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnClientFactory : ISlnClientFactory
{
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnFormulaEntityService _formulas;
    private readonly ISlnTreatmentRecordEntityService _treatmentRecords;
    private readonly ISlnClientPhotoEntityService _photos;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnClientFactory> _logger;

    public SlnClientFactory(
        ISlnClientEntityService clients,
        ISlnFormulaEntityService formulas,
        ISlnTreatmentRecordEntityService treatmentRecords,
        ISlnClientPhotoEntityService photos,
        ISlnAppointmentEntityService appointments,
        ISlnInvoiceEntityService invoices,
        IUnitOfWork uow,
        ILogger<SlnClientFactory> logger)
    {
        _clients = clients;
        _formulas = formulas;
        _treatmentRecords = treatmentRecords;
        _photos = photos;
        _appointments = appointments;
        _invoices = invoices;
        _uow = uow;
        _logger = logger;
    }

    public async Task<object> GetClientsAsync(int customerId, string? search, int? branchId = null, int page = 1, int pageSize = 50)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.Email != null && c.Email.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var clients = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var clientIds = clients.Select(c => c.Id).ToList();

        // Ziyaret ve harcama istatistikleri
        var invoiceStats = await _invoices.GetAllQueryable()
            .Where(i => clientIds.Contains(i.SlnClientId ?? 0)
                        && i.StatusId != 3
                        && (!branchId.HasValue || i.BranchId == branchId.Value))
            .GroupBy(i => i.SlnClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                VisitCount = g.Count(),
                TotalSpent = g.Sum(i => i.NetAmount),
                LastVisit = g.Max(i => i.InvoiceDate)
            })
            .ToListAsync();

        var statsDict = invoiceStats.ToDictionary(s => s.ClientId ?? 0);

        var items = clients.Select(c =>
        {
            statsDict.TryGetValue(c.Id, out var stats);
            return new SlnClientDto
            {
                Id = c.Id,
                Uid = c.Uid,
                BranchId = c.BranchId,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                GenderId = c.GenderId,
                BirthDate = c.BirthDate,
                HairColor = c.HairColor,
                IsFavorite = c.IsFavorite,
                HealthInfoRequiresReview = c.HealthInfoRequiresReview,
                CreatedAt = c.CreatedAt,
                VisitCount = stats?.VisitCount ?? 0,
                TotalSpent = stats?.TotalSpent ?? 0,
                LastVisit = stats?.LastVisit
            };
        }).ToList();

        return new { items, totalCount, page, pageSize };
    }

    public async Task<SlnClientDetailDto?> GetClientDetailAsync(int clientId, int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query
            .Include(c => c.Formulas).ThenInclude(f => f.AppliedByPersonnel).ThenInclude(p => p!.User)
            .Include(c => c.HealthInfoReviewedByPersonnel).ThenInclude(p => p!.User)
            .Include(c => c.Photos)
            .FirstOrDefaultAsync();

        if (client == null) return null;

        // Istatistikler
        var invoiceStats = await _invoices.GetAllQueryable()
            .Where(i => i.SlnClientId == clientId
                        && i.StatusId != 3
                        && (!branchId.HasValue || i.BranchId == branchId.Value))
            .GroupBy(i => i.SlnClientId)
            .Select(g => new
            {
                VisitCount = g.Count(),
                TotalSpent = g.Sum(i => i.NetAmount),
                LastVisit = g.Max(i => i.InvoiceDate)
            })
            .FirstOrDefaultAsync();

        return new SlnClientDetailDto
        {
            Id = client.Id,
            Uid = client.Uid,
            BranchId = client.BranchId,
            FullName = client.FullName,
            Phone = client.Phone,
            Phone2 = client.Phone2,
            Email = client.Email,
            GenderId = client.GenderId,
            BirthDate = client.BirthDate,
            MarriageDate = client.MarriageDate,
            Occupation = client.Occupation,
            City = client.City,
            Address = client.Address,
            HairColor = client.HairColor,
            WhiteRatioPercent = client.WhiteRatioPercent,
            SkinType = client.SkinType,
            SkinSensitivity = client.SkinSensitivity,
            Allergies = client.Allergies,
            Contraindications = client.Contraindications,
            MedicalNotes = client.MedicalNotes,
            HealthInfoRequiresReview = client.HealthInfoRequiresReview,
            HealthInfoUpdatedAt = client.HealthInfoUpdatedAt,
            HealthInfoReviewedAt = client.HealthInfoReviewedAt,
            HealthInfoReviewedByName = client.HealthInfoReviewedByPersonnel?.User?.FullName,
            Notes = client.Notes,
            IsFavorite = client.IsFavorite,
            CreatedAt = client.CreatedAt,
            VisitCount = invoiceStats?.VisitCount ?? 0,
            TotalSpent = invoiceStats?.TotalSpent ?? 0,
            LastVisit = invoiceStats?.LastVisit,
            Formulas = client.Formulas.OrderByDescending(f => f.AppliedAt).Select(f => new SlnFormulaDto
            {
                Id = f.Id,
                FormulaText = f.FormulaText,
                ColorCode = f.ColorCode,
                OxidantRatio = f.OxidantRatio,
                ApplicationNotes = f.ApplicationNotes,
                AppliedByName = f.AppliedByPersonnel?.User?.FullName,
                AppliedAt = f.AppliedAt
            }).ToList(),
            Photos = client.Photos.OrderByDescending(p => p.TakenAt).Select(p => new SlnClientPhotoDto
            {
                Id = p.Id,
                FilePath = p.FilePath,
                Description = p.Description,
                TakenAt = p.TakenAt
            }).ToList(),
            TreatmentRecords = await GetTreatmentRecordsForClientAsync(clientId, customerId, branchId)
        };
    }

    public async Task<SlnClientDto> CreateClientAsync(SlnClientCreateDto dto, int customerId, int? branchId = null)
    {
        var client = new SlnClient
        {
            CustomerId = customerId,
            BranchId = branchId,
            FullName = dto.FullName,
            Phone = Shared.Helpers.PhoneHelper.Normalize(dto.Phone),
            Phone2 = Shared.Helpers.PhoneHelper.Normalize(dto.Phone2),
            Email = dto.Email,
            GenderId = dto.GenderId,
            BirthDate = dto.BirthDate,
            MarriageDate = dto.MarriageDate,
            Occupation = dto.Occupation,
            City = dto.City,
            Address = dto.Address,
            HairColor = dto.HairColor,
            WhiteRatioPercent = dto.WhiteRatioPercent,
            SkinType = dto.SkinType,
            SkinSensitivity = dto.SkinSensitivity,
            Allergies = dto.Allergies,
            Contraindications = dto.Contraindications,
            MedicalNotes = dto.MedicalNotes,
            HealthInfoUpdatedAt = HasHealthInfo(dto) ? DateTime.UtcNow : null,
            Notes = dto.Notes
        };

        _clients.Add(client);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni salon musterisi olusturuldu: {ClientId} - {FullName}", client.Id, client.FullName);

        return new SlnClientDto
        {
            Id = client.Id,
            Uid = client.Uid,
            BranchId = client.BranchId,
            FullName = client.FullName,
            Phone = client.Phone,
            Email = client.Email,
            GenderId = client.GenderId,
            BirthDate = client.BirthDate,
            HairColor = client.HairColor,
            IsFavorite = client.IsFavorite,
            HealthInfoRequiresReview = client.HealthInfoRequiresReview,
            CreatedAt = client.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> UpdateClientAsync(int clientId, SlnClientUpdateDto dto, int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null) return (false, "Musteri bulunamadi");

        if (client.BranchId == null && branchId.HasValue)
            client.BranchId = branchId;

        client.FullName = dto.FullName;
        client.Phone = Shared.Helpers.PhoneHelper.Normalize(dto.Phone);
        client.Phone2 = Shared.Helpers.PhoneHelper.Normalize(dto.Phone2);
        client.Email = dto.Email;
        client.GenderId = dto.GenderId;
        client.BirthDate = dto.BirthDate;
        client.MarriageDate = dto.MarriageDate;
        client.Occupation = dto.Occupation;
        client.City = dto.City;
        client.Address = dto.Address;
        client.HairColor = dto.HairColor;
        client.WhiteRatioPercent = dto.WhiteRatioPercent;
        client.SkinType = dto.SkinType;
        client.SkinSensitivity = dto.SkinSensitivity;
        client.Allergies = dto.Allergies;
        client.Contraindications = dto.Contraindications;
        client.MedicalNotes = dto.MedicalNotes;
        client.HealthInfoUpdatedAt = HasHealthInfo(dto) ? DateTime.UtcNow : client.HealthInfoUpdatedAt;
        client.Notes = dto.Notes;
        client.IsFavorite = dto.IsFavorite;
        client.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateHealthInfoAsync(
        int clientId,
        SlnClientHealthUpdateDto dto,
        int customerId,
        bool requiresReview,
        int? reviewedByPersonnelId = null,
        int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null) return (false, "Musteri bulunamadi");

        client.SkinType = dto.SkinType;
        client.SkinSensitivity = dto.SkinSensitivity;
        client.Allergies = dto.Allergies;
        client.Contraindications = dto.Contraindications;
        client.MedicalNotes = dto.MedicalNotes;
        client.HealthInfoUpdatedAt = DateTime.UtcNow;
        client.HealthInfoRequiresReview = requiresReview;
        if (!requiresReview && reviewedByPersonnelId.HasValue)
        {
            client.HealthInfoReviewedAt = DateTime.UtcNow;
            client.HealthInfoReviewedByPersonnelId = reviewedByPersonnelId;
        }
        client.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReviewHealthInfoAsync(int clientId, int customerId, int reviewedByPersonnelId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null) return (false, "Musteri bulunamadi");

        client.HealthInfoRequiresReview = false;
        client.HealthInfoReviewedAt = DateTime.UtcNow;
        client.HealthInfoReviewedByPersonnelId = reviewedByPersonnelId;
        client.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteClientAsync(int clientId, int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null) return (false, "Musteri bulunamadi");

        _clients.Remove(client);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Salon musterisi silindi: {ClientId}", clientId);
        return (true, null);
    }

    public async Task<SlnClientSuggestionsDto> GetSuggestionsAsync(int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var clients = await query
            .Select(c => new { c.HairColor, c.SkinType })
            .ToListAsync();

        return new SlnClientSuggestionsDto
        {
            HairColors = clients.Where(c => !string.IsNullOrWhiteSpace(c.HairColor)).Select(c => c.HairColor!).Distinct().OrderBy(x => x).ToList(),
            SkinTypes = clients.Where(c => !string.IsNullOrWhiteSpace(c.SkinType)).Select(c => c.SkinType!).Distinct().OrderBy(x => x).ToList()
        };
    }

    public async Task<SlnFormulaDto> AddFormulaAsync(SlnFormulaCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        // Musterinin bu firmaya ait oldugunu dogrula
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == dto.SlnClientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null)
            throw new InvalidOperationException("Musteri bulunamadi");

        var formula = new SlnFormula
        {
            SlnClientId = dto.SlnClientId,
            FormulaText = dto.FormulaText,
            ColorCode = dto.ColorCode,
            OxidantRatio = dto.OxidantRatio,
            ApplicationNotes = dto.ApplicationNotes,
            AppliedByPersonnelId = userId
        };

        _formulas.Add(formula);
        await _uow.SaveChangesAsync();

        return new SlnFormulaDto
        {
            Id = formula.Id,
            FormulaText = formula.FormulaText,
            ColorCode = formula.ColorCode,
            OxidantRatio = formula.OxidantRatio,
            ApplicationNotes = formula.ApplicationNotes,
            AppliedAt = formula.AppliedAt
        };
    }

    public async Task<(bool Success, string? Error)> DeleteFormulaAsync(int formulaId, int customerId, int? branchId = null)
    {
        var formulaQuery = _formulas.GetAllQueryable()
            .Include(f => f.SlnClient)
            .Where(f => f.Id == formulaId && f.SlnClient != null && f.SlnClient.CustomerId == customerId);
        if (branchId.HasValue)
        {
            var id = branchId.Value;
            formulaQuery = formulaQuery.Where(f =>
                f.SlnClient!.BranchId == id
                || f.SlnClient.Appointments.Any(a => a.BranchId == id)
                || f.SlnClient.Invoices.Any(i => i.BranchId == id));
        }

        var formula = await formulaQuery.FirstOrDefaultAsync();

        if (formula == null) return (false, "Formul bulunamadi");

        _formulas.Remove(formula);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SlnTreatmentRecordDto> AddTreatmentRecordAsync(SlnTreatmentRecordCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == dto.SlnClientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null)
            throw new InvalidOperationException("Musteri bulunamadi");

        SlnAppointment? appointment = null;
        int? appointmentServiceId = null;
        if (dto.SlnAppointmentId.HasValue)
        {
            appointment = await _appointments.GetAllQueryable()
                .Include(a => a.Services)
                .FirstOrDefaultAsync(a => a.Id == dto.SlnAppointmentId.Value
                                       && a.CustomerId == customerId
                                       && a.SlnClientId == dto.SlnClientId
                                       && (!branchId.HasValue || a.BranchId == branchId.Value));
            if (appointment == null)
                throw new InvalidOperationException("Randevu bulunamadi");

            var appointmentServiceIds = appointment.Services
                .OrderBy(s => s.SortOrder)
                .Select(s => s.SlnServiceId)
                .ToList();
            if (appointment.ServiceId.HasValue && !appointmentServiceIds.Contains(appointment.ServiceId.Value))
                appointmentServiceIds.Insert(0, appointment.ServiceId.Value);

            if (dto.ServiceId.HasValue && appointmentServiceIds.Count > 0 && !appointmentServiceIds.Contains(dto.ServiceId.Value))
                throw new InvalidOperationException("Secilen hizmet randevuya ait degil");

            if (appointmentServiceIds.Count > 0)
                appointmentServiceId = appointmentServiceIds[0];
        }

        var record = new SlnTreatmentRecord
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            SlnAppointmentId = dto.SlnAppointmentId,
            ServiceId = dto.ServiceId ?? appointmentServiceId,
            PersonnelId = dto.PersonnelId ?? appointment?.PersonnelId,
            TreatmentDate = dto.TreatmentDate ?? appointment?.StartTime ?? DateTime.UtcNow,
            SkinTypeSnapshot = client.SkinType,
            AllergiesSnapshot = client.Allergies,
            ContraindicationsSnapshot = client.Contraindications,
            SessionNotes = dto.SessionNotes,
            DeviceParameters = dto.DeviceParameters,
            ProductNotes = dto.ProductNotes,
            AftercareNotes = dto.AftercareNotes,
            CreatedByPersonnelId = userId
        };

        _treatmentRecords.Add(record);
        await _uow.SaveChangesAsync();

        var mapped = await _treatmentRecords.GetAllQueryable()
            .Include(r => r.Service)
            .Include(r => r.Personnel).ThenInclude(p => p!.User)
            .FirstAsync(r => r.Id == record.Id);
        return MapTreatmentRecord(mapped);
    }

    public async Task<(bool Success, string? Error)> DeleteTreatmentRecordAsync(int recordId, int customerId, int? branchId = null)
    {
        var query = _treatmentRecords.GetAllQueryable()
            .Include(r => r.SlnClient)
            .Where(r => r.Id == recordId && r.CustomerId == customerId);
        if (branchId.HasValue)
        {
            var id = branchId.Value;
            query = query.Where(r =>
                (r.SlnAppointment != null && r.SlnAppointment.BranchId == id)
                || (r.SlnClient != null
                    && (r.SlnClient.BranchId == id
                        || r.SlnClient.Appointments.Any(a => a.BranchId == id)
                        || r.SlnClient.Invoices.Any(i => i.BranchId == id))));
        }

        var record = await query.FirstOrDefaultAsync();

        if (record == null) return (false, "Seans kaydi bulunamadi");

        _treatmentRecords.Remove(record);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnblockClientAsync(int clientId, int customerId, int? branchId = null)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.Id == clientId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var client = await query.FirstOrDefaultAsync();

        if (client == null) return (false, "Musteri bulunamadi");

        client.IsBlacklisted = false;
        client.NoShowCount = 0;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task<List<SlnTreatmentRecordDto>> GetTreatmentRecordsForClientAsync(int clientId, int customerId, int? branchId = null)
    {
        var query = _treatmentRecords.GetAllQueryable()
            .Include(r => r.Service)
            .Include(r => r.Personnel).ThenInclude(p => p!.User)
            .Where(r => r.CustomerId == customerId && r.SlnClientId == clientId);
        if (branchId.HasValue)
        {
            var id = branchId.Value;
            query = query.Where(r =>
                (r.SlnAppointment != null && r.SlnAppointment.BranchId == id)
                || (r.SlnClient != null
                    && (r.SlnClient.BranchId == id
                        || r.SlnClient.Appointments.Any(a => a.BranchId == id)
                        || r.SlnClient.Invoices.Any(i => i.BranchId == id))));
        }

        var records = await query
            .OrderByDescending(r => r.TreatmentDate)
            .Take(50)
            .ToListAsync();

        return records.Select(MapTreatmentRecord).ToList();
    }

    private static SlnTreatmentRecordDto MapTreatmentRecord(SlnTreatmentRecord r) => new()
    {
        Id = r.Id,
        SlnClientId = r.SlnClientId,
        SlnAppointmentId = r.SlnAppointmentId,
        ServiceId = r.ServiceId,
        ServiceName = r.Service?.Name,
        PersonnelId = r.PersonnelId,
        PersonnelName = r.Personnel?.User?.FullName ?? r.Personnel?.Title,
        TreatmentDate = r.TreatmentDate,
        SkinTypeSnapshot = r.SkinTypeSnapshot,
        AllergiesSnapshot = r.AllergiesSnapshot,
        ContraindicationsSnapshot = r.ContraindicationsSnapshot,
        SessionNotes = r.SessionNotes,
        DeviceParameters = r.DeviceParameters,
        ProductNotes = r.ProductNotes,
        AftercareNotes = r.AftercareNotes,
        CreatedAt = r.CreatedAt
    };

    private static bool HasHealthInfo(SlnClientCreateDto dto)
        => !string.IsNullOrWhiteSpace(dto.SkinType)
        || !string.IsNullOrWhiteSpace(dto.SkinSensitivity)
        || !string.IsNullOrWhiteSpace(dto.Allergies)
        || !string.IsNullOrWhiteSpace(dto.Contraindications)
        || !string.IsNullOrWhiteSpace(dto.MedicalNotes);
}
