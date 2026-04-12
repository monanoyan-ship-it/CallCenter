using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnBeforeAfterFactory : ISlnBeforeAfterFactory
{
    private readonly ISlnBeforeAfterPhotoEntityService _photoEs;
    private readonly IUnitOfWork _uow;

    public SlnBeforeAfterFactory(ISlnBeforeAfterPhotoEntityService photoEs, IUnitOfWork uow)
    {
        _photoEs = photoEs;
        _uow = uow;
    }

    public async Task<List<SlnBeforeAfterPhotoDto>> GetPhotosAsync(int customerId)
    {
        return await _photoEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.SlnClient)
            .Include(p => p.Service)
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    public async Task<SlnBeforeAfterPhotoDto?> GetPhotoAsync(int id, int customerId)
    {
        var photo = await _photoEs.GetAllQueryable()
            .Include(p => p.SlnClient)
            .Include(p => p.Service)
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        return photo != null ? MapToDto(photo) : null;
    }

    public async Task<SlnBeforeAfterPhotoDto> CreatePhotoAsync(SlnBeforeAfterPhotoCreateDto dto, int customerId)
    {
        var photo = new SlnBeforeAfterPhoto
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            ServiceId = dto.ServiceId,
            BeforePhotoUrl = dto.BeforePhotoUrl,
            AfterPhotoUrl = dto.AfterPhotoUrl,
            Notes = dto.Notes,
            PersonnelId = dto.PersonnelId,
            IsPublic = dto.IsPublic
        };
        _photoEs.Add(photo);
        await _uow.SaveChangesAsync();
        return (await GetPhotoAsync(photo.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdatePhotoAsync(int id, SlnBeforeAfterPhotoUpdateDto dto, int customerId)
    {
        var photo = await _photoEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (photo == null) return (false, "Fotograf bulunamadi");

        photo.SlnClientId = dto.SlnClientId;
        photo.ServiceId = dto.ServiceId;
        photo.BeforePhotoUrl = dto.BeforePhotoUrl;
        photo.AfterPhotoUrl = dto.AfterPhotoUrl;
        photo.Notes = dto.Notes;
        photo.PersonnelId = dto.PersonnelId;
        photo.IsPublic = dto.IsPublic;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePhotoAsync(int id, int customerId)
    {
        var photo = await _photoEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (photo == null) return (false, "Fotograf bulunamadi");

        _photoEs.Remove(photo);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnBeforeAfterPhotoDto MapToDto(SlnBeforeAfterPhoto p) => new()
    {
        Id = p.Id,
        SlnClientId = p.SlnClientId,
        ClientName = p.SlnClient?.FullName ?? "",
        ServiceId = p.ServiceId,
        ServiceName = p.Service?.Name,
        BeforePhotoUrl = p.BeforePhotoUrl,
        AfterPhotoUrl = p.AfterPhotoUrl,
        Notes = p.Notes,
        PersonnelId = p.PersonnelId,
        PersonnelName = p.Personnel?.User?.FullName,
        IsPublic = p.IsPublic,
        CreatedAt = p.CreatedAt
    };
}
