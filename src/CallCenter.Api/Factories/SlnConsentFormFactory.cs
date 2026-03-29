using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnConsentFormFactory : ISlnConsentFormFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnConsentFormFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<SlnConsentFormDto>> GetFormsAsync(int customerId)
    {
        return await _db.SlnConsentForms
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new SlnConsentFormDto
            {
                Id = f.Id,
                Title = f.Title,
                HtmlContent = f.HtmlContent,
                RequireSignature = f.RequireSignature,
                IsActive = f.IsActive,
                SignedCount = _db.SlnClientConsents.Count(c => c.FormId == f.Id),
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SlnConsentFormDto?> GetFormAsync(int id, int customerId)
    {
        var form = await _db.SlnConsentForms
            .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
        if (form == null) return null;

        return new SlnConsentFormDto
        {
            Id = form.Id,
            Title = form.Title,
            HtmlContent = form.HtmlContent,
            RequireSignature = form.RequireSignature,
            IsActive = form.IsActive,
            SignedCount = await _db.SlnClientConsents.CountAsync(c => c.FormId == form.Id),
            CreatedAt = form.CreatedAt
        };
    }

    public async Task<SlnConsentFormDto> CreateFormAsync(SlnConsentFormCreateDto dto, int customerId)
    {
        var form = new SlnConsentForm
        {
            CustomerId = customerId,
            Title = dto.Title,
            HtmlContent = dto.HtmlContent,
            RequireSignature = dto.RequireSignature,
            IsActive = dto.IsActive
        };
        _db.SlnConsentForms.Add(form);
        await _uow.SaveChangesAsync();
        return (await GetFormAsync(form.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateFormAsync(int id, SlnConsentFormUpdateDto dto, int customerId)
    {
        var form = await _db.SlnConsentForms.FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
        if (form == null) return (false, "Form bulunamadi");

        form.Title = dto.Title;
        form.HtmlContent = dto.HtmlContent;
        form.RequireSignature = dto.RequireSignature;
        form.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteFormAsync(int id, int customerId)
    {
        var form = await _db.SlnConsentForms.FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
        if (form == null) return (false, "Form bulunamadi");

        _db.SlnConsentForms.Remove(form);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnClientConsentDto>> GetSignedConsentsAsync(int customerId, int? formId = null)
    {
        var query = _db.SlnClientConsents
            .Include(c => c.Form)
            .Include(c => c.SlnClient)
            .Where(c => c.Form != null && c.Form.CustomerId == customerId);

        if (formId.HasValue)
            query = query.Where(c => c.FormId == formId.Value);

        return await query
            .OrderByDescending(c => c.SignedAt)
            .Select(c => new SlnClientConsentDto
            {
                Id = c.Id,
                FormId = c.FormId,
                FormTitle = c.Form != null ? c.Form.Title : "",
                SlnClientId = c.SlnClientId,
                ClientName = c.SlnClient != null ? c.SlnClient.FullName : "",
                IpAddress = c.IpAddress,
                SignedAt = c.SignedAt
            })
            .ToListAsync();
    }

    public async Task<SlnClientConsentDto> CreateConsentAsync(SlnClientConsentCreateDto dto)
    {
        var consent = new SlnClientConsent
        {
            FormId = dto.FormId,
            SlnClientId = dto.SlnClientId,
            SignatureData = dto.SignatureData,
            IpAddress = dto.IpAddress
        };
        _db.SlnClientConsents.Add(consent);
        await _uow.SaveChangesAsync();

        var form = await _db.SlnConsentForms.FindAsync(dto.FormId);
        var client = await _db.SlnClients.FindAsync(dto.SlnClientId);

        return new SlnClientConsentDto
        {
            Id = consent.Id,
            FormId = consent.FormId,
            FormTitle = form?.Title ?? "",
            SlnClientId = consent.SlnClientId,
            ClientName = client?.FullName ?? "",
            IpAddress = consent.IpAddress,
            SignedAt = consent.SignedAt
        };
    }
}
