using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PlatformEmailTemplateFactory : IPlatformEmailTemplateFactory
{
    private readonly IPlatformEmailTemplateEntityService _emailEs;
    private readonly IUnitOfWork _uow;

    public PlatformEmailTemplateFactory(IPlatformEmailTemplateEntityService emailEs, IUnitOfWork uow)
    {
        _emailEs = emailEs;
        _uow = uow;
    }

    // ─── Event CRUD ───

    public async Task<List<PlatformEmailEventDto>> GetAllEventsAsync()
    {
        return await _emailEs.GetAllEventsQueryable()
            .OrderBy(e => e.ProductType)
            .ThenBy(e => e.EventKey)
            .Select(e => MapEventToDto(e))
            .ToListAsync();
    }

    public async Task<PlatformEmailEventDto?> GetEventByIdAsync(int id)
    {
        var ev = await _emailEs.GetEventByIdAsync(id);
        return ev != null ? MapEventToDto(ev) : null;
    }

    public async Task<PlatformEmailEventDto> CreateEventAsync(PlatformEmailEventCreateDto dto)
    {
        var ev = new PlatformEmailEvent
        {
            EventKey = dto.EventKey,
            ProductType = dto.ProductType,
            Description = dto.Description,
            AvailablePlaceholders = dto.AvailablePlaceholders,
            IsActive = dto.IsActive
        };
        _emailEs.AddEvent(ev);
        await _uow.SaveChangesAsync();
        return MapEventToDto(ev);
    }

    public async Task<(bool Success, string? Error)> UpdateEventAsync(int id, PlatformEmailEventUpdateDto dto)
    {
        var ev = await _emailEs.GetEventByIdAsync(id);
        if (ev == null) return (false, "Olay bulunamadi");

        if (dto.ProductType != null) ev.ProductType = dto.ProductType;
        if (dto.Description != null) ev.Description = dto.Description;
        if (dto.AvailablePlaceholders != null) ev.AvailablePlaceholders = dto.AvailablePlaceholders;
        if (dto.IsActive.HasValue) ev.IsActive = dto.IsActive.Value;
        ev.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteEventAsync(int id)
    {
        var ev = await _emailEs.GetAllEventsQueryable()
            .Include(e => e.Templates)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null) return (false, "Olay bulunamadi");

        _emailEs.RemoveEvent(ev); // Cascade delete templates
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ─── Template CRUD ───

    public async Task<PlatformEmailTemplateDto> AddTemplateAsync(int eventId, PlatformEmailTemplateCreateDto dto)
    {
        var template = new PlatformEmailTemplate
        {
            EventId = eventId,
            Language = dto.Language,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            IsActive = dto.IsActive
        };
        _emailEs.AddTemplate(template);
        await _uow.SaveChangesAsync();
        return MapTemplateToDto(template);
    }

    public async Task<(bool Success, string? Error)> UpdateTemplateAsync(int templateId, PlatformEmailTemplateUpdateDto dto)
    {
        var template = await _emailEs.GetTemplateByIdAsync(templateId);
        if (template == null) return (false, "Taslak bulunamadi");

        if (dto.Subject != null) template.Subject = dto.Subject;
        if (dto.HtmlBody != null) template.HtmlBody = dto.HtmlBody;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        template.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteTemplateAsync(int templateId)
    {
        var template = await _emailEs.GetTemplateByIdAsync(templateId);
        if (template == null) return (false, "Taslak bulunamadi");

        _emailEs.RemoveTemplate(template);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ─── Mapping ───

    private static PlatformEmailEventDto MapEventToDto(PlatformEmailEvent e) => new()
    {
        Id = e.Id,
        EventKey = e.EventKey,
        ProductType = e.ProductType,
        Description = e.Description,
        AvailablePlaceholders = e.AvailablePlaceholders,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        Templates = e.Templates.Select(MapTemplateToDto).ToList()
    };

    private static PlatformEmailTemplateDto MapTemplateToDto(PlatformEmailTemplate t) => new()
    {
        Id = t.Id,
        EventId = t.EventId,
        Language = t.Language,
        Subject = t.Subject,
        HtmlBody = t.HtmlBody,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
