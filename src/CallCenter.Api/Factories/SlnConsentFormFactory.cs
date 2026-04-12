using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnConsentFormFactory : ISlnConsentFormFactory
{
    private readonly ISlnConsentFormEntityService _formEs;
    private readonly ISlnClientConsentEntityService _consentEs;
    private readonly ISlnClientEntityService _clientEs;
    private readonly IUnitOfWork _uow;

    public SlnConsentFormFactory(ISlnConsentFormEntityService formEs, ISlnClientConsentEntityService consentEs, ISlnClientEntityService clientEs, IUnitOfWork uow)
    {
        _formEs = formEs;
        _consentEs = consentEs;
        _clientEs = clientEs;
        _uow = uow;
    }

    public async Task<List<SlnConsentFormDto>> GetFormsAsync(int customerId)
    {
        return await _formEs.GetAllQueryable()
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new SlnConsentFormDto
            {
                Id = f.Id,
                Title = f.Title,
                HtmlContent = f.HtmlContent,
                RequireSignature = f.RequireSignature,
                IsActive = f.IsActive,
                SignedCount = _consentEs.GetAllQueryable().Count(c => c.FormId == f.Id),
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SlnConsentFormDto?> GetFormAsync(int id, int customerId)
    {
        var form = await _formEs.GetAllQueryable()
            .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
        if (form == null) return null;

        return new SlnConsentFormDto
        {
            Id = form.Id,
            Title = form.Title,
            HtmlContent = form.HtmlContent,
            RequireSignature = form.RequireSignature,
            IsActive = form.IsActive,
            SignedCount = await _consentEs.GetAllQueryable().CountAsync(c => c.FormId == form.Id),
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
        _formEs.Add(form);
        await _uow.SaveChangesAsync();
        return (await GetFormAsync(form.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateFormAsync(int id, SlnConsentFormUpdateDto dto, int customerId)
    {
        var form = await _formEs.GetAllQueryable().FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
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
        var form = await _formEs.GetAllQueryable().FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);
        if (form == null) return (false, "Form bulunamadi");

        _formEs.Remove(form);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnClientConsentDto>> GetSignedConsentsAsync(int customerId, int? formId = null)
    {
        var query = _consentEs.GetAllQueryable()
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
        _consentEs.Add(consent);
        await _uow.SaveChangesAsync();

        var form = await _formEs.GetByIdAsync(dto.FormId);
        var client = await _clientEs.GetByIdAsync(dto.SlnClientId);

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
