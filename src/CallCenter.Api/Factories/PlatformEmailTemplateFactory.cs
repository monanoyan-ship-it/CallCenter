using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PlatformEmailTemplateFactory : IPlatformEmailTemplateFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public PlatformEmailTemplateFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<PlatformEmailTemplateDto>> GetAllAsync()
    {
        return await _db.PlatformEmailTemplates
            .OrderBy(t => t.EventKey)
            .ThenBy(t => t.Language)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<PlatformEmailTemplateDto?> GetByIdAsync(int id)
    {
        var template = await _db.PlatformEmailTemplates.FindAsync(id);
        return template != null ? MapToDto(template) : null;
    }

    public async Task<PlatformEmailTemplateDto> CreateAsync(PlatformEmailTemplateCreateDto dto)
    {
        var template = new PlatformEmailTemplate
        {
            EventKey = dto.EventKey,
            ProductType = dto.ProductType,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            IsActive = dto.IsActive,
            Description = dto.Description,
            AvailablePlaceholders = dto.AvailablePlaceholders,
            Language = dto.Language
        };
        _db.PlatformEmailTemplates.Add(template);
        await _uow.SaveChangesAsync();
        return MapToDto(template);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, PlatformEmailTemplateUpdateDto dto)
    {
        var template = await _db.PlatformEmailTemplates.FindAsync(id);
        if (template == null) return (false, "Taslak bulunamadi");

        if (dto.ProductType != null) template.ProductType = dto.ProductType;
        if (dto.Subject != null) template.Subject = dto.Subject;
        if (dto.HtmlBody != null) template.HtmlBody = dto.HtmlBody;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        if (dto.Description != null) template.Description = dto.Description;
        if (dto.AvailablePlaceholders != null) template.AvailablePlaceholders = dto.AvailablePlaceholders;
        if (dto.Language != null) template.Language = dto.Language;
        template.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var template = await _db.PlatformEmailTemplates.FindAsync(id);
        if (template == null) return (false, "Taslak bulunamadi");

        _db.PlatformEmailTemplates.Remove(template);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static PlatformEmailTemplateDto MapToDto(PlatformEmailTemplate t) => new()
    {
        Id = t.Id,
        Uid = t.Uid,
        EventKey = t.EventKey,
        ProductType = t.ProductType,
        Subject = t.Subject,
        HtmlBody = t.HtmlBody,
        IsActive = t.IsActive,
        Description = t.Description,
        AvailablePlaceholders = t.AvailablePlaceholders,
        Language = t.Language,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
