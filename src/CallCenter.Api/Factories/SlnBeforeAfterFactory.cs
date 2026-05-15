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
    private readonly ISlnClientEntityService _clientEs;
    private readonly IUnitOfWork _uow;

    public SlnBeforeAfterFactory(
        ISlnBeforeAfterPhotoEntityService photoEs,
        ISlnClientEntityService clientEs,
        IUnitOfWork uow)
    {
        _photoEs = photoEs;
        _clientEs = clientEs;
        _uow = uow;
    }

    public async Task<List<SlnBeforeAfterPhotoDto>> GetPhotosAsync(int customerId, int? branchId = null)
    {
        var query = SalonBranchScope.ApplyToBeforeAfterPhotos(
            _photoEs.GetAllQueryable().Where(p => p.CustomerId == customerId),
            branchId);

        return await query
            .Include(p => p.SlnClient)
            .Include(p => p.Service)
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    public async Task<SlnBeforeAfterPhotoDto?> GetPhotoAsync(int id, int customerId, int? branchId = null)
    {
        var photo = await SalonBranchScope.ApplyToBeforeAfterPhotos(
                _photoEs.GetAllQueryable().Where(p => p.Id == id && p.CustomerId == customerId),
                branchId)
            .Include(p => p.SlnClient)
            .Include(p => p.Service)
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .FirstOrDefaultAsync();
        return photo != null ? MapToDto(photo) : null;
    }

    public async Task<SlnBeforeAfterPhotoDto> CreatePhotoAsync(SlnBeforeAfterPhotoCreateDto dto, int customerId, int? branchId = null)
    {
        var clientExists = await SalonBranchScope.ApplyToClients(
                _clientEs.GetAllQueryable().Where(c => c.Id == dto.SlnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists)
        {
            throw new InvalidOperationException("Musteri bulunamadi");
        }

        var photo = new SlnBeforeAfterPhoto
        {
            CustomerId = customerId,
            BranchId = branchId,
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
        return (await GetPhotoAsync(photo.Id, customerId, branchId))!;
    }

    public async Task<(bool Success, string? Error)> UpdatePhotoAsync(int id, SlnBeforeAfterPhotoUpdateDto dto, int customerId, int? branchId = null)
    {
        var photo = await SalonBranchScope.ApplyToBeforeAfterPhotos(
                _photoEs.GetAllQueryable().Where(p => p.Id == id && p.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (photo == null) return (false, "Fotograf bulunamadi");

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clientEs.GetAllQueryable().Where(c => c.Id == dto.SlnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists) return (false, "Musteri bulunamadi");

        if (photo.BranchId == null && branchId.HasValue)
        {
            photo.BranchId = branchId;
        }
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

    public async Task<(bool Success, string? Error)> DeletePhotoAsync(int id, int customerId, int? branchId = null)
    {
        var photo = await SalonBranchScope.ApplyToBeforeAfterPhotos(
                _photoEs.GetAllQueryable().Where(p => p.Id == id && p.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
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
        BranchId = p.BranchId,
        CreatedAt = p.CreatedAt
    };
}
