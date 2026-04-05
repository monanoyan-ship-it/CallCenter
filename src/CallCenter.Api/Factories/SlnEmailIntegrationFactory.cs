using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnEmailIntegrationFactory : ISlnEmailIntegrationFactory
{
    private readonly ICustomerEmailIntegrationEntityService _es;
    private readonly IUnitOfWork _uow;
    private readonly AesEncryptionService _aes;
    private readonly IEmailSendService _emailSend;

    public SlnEmailIntegrationFactory(
        ICustomerEmailIntegrationEntityService es,
        IUnitOfWork uow,
        AesEncryptionService aes,
        IEmailSendService emailSend)
    {
        _es = es;
        _uow = uow;
        _aes = aes;
        _emailSend = emailSend;
    }

    public async Task<List<EmailIntegrationListDto>> GetIntegrationsAsync(int customerId)
    {
        var list = await _es.GetByCustomerQueryable(customerId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return list.Select(MapToListDto).ToList();
    }

    public async Task<EmailIntegrationDetailDto?> GetIntegrationDetailAsync(int customerId, Guid uid)
    {
        var entity = await _es.GetByUidAsync(uid);
        if (entity == null || entity.CustomerId != customerId) return null;

        var dto = new EmailIntegrationDetailDto
        {
            LastTestError = entity.LastTestError
        };
        CopyListFields(entity, dto);
        return dto;
    }

    public async Task<(int Id, string? Error)> CreateIntegrationAsync(int customerId, CreateEmailIntegrationRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SenderEmail))
            return (0, "Gonderici e-posta adresi zorunludur.");

        if (EmailProviders.GetById(dto.ProviderTypeId) == null)
            return (0, "Gecersiz provider tipi.");

        var credsJson = JsonSerializer.Serialize(dto.Credentials);

        var entity = new CustomerEmailIntegration
        {
            CustomerId = customerId,
            ProviderTypeId = dto.ProviderTypeId,
            DisplayName = dto.DisplayName,
            SenderEmail = dto.SenderEmail,
            SenderName = dto.SenderName,
            EncryptedCredentials = _aes.Encrypt(credsJson),
            IsDefault = dto.IsDefault,
            IsActive = true
        };

        if (dto.IsDefault)
            await ClearDefaultAsync(customerId);

        _es.Add(entity);
        await _uow.SaveChangesAsync();
        return (entity.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateIntegrationAsync(int customerId, Guid uid, UpdateEmailIntegrationRequest dto)
    {
        var entity = await _es.GetByUidAsync(uid);
        if (entity == null || entity.CustomerId != customerId)
            return (false, "Entegrasyon bulunamadi.");

        if (dto.DisplayName != null) entity.DisplayName = dto.DisplayName;
        if (dto.SenderName != null) entity.SenderName = dto.SenderName;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

        if (dto.IsDefault == true)
        {
            await ClearDefaultAsync(customerId);
            entity.IsDefault = true;
        }
        else if (dto.IsDefault == false)
        {
            entity.IsDefault = false;
        }

        if (dto.Credentials != null && dto.Credentials.Count > 0)
        {
            var existingJson = _aes.Decrypt(entity.EncryptedCredentials);
            var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson) ?? new();
            foreach (var kv in dto.Credentials)
                existing[kv.Key] = kv.Value;
            entity.EncryptedCredentials = _aes.Encrypt(JsonSerializer.Serialize(existing));
        }

        entity.UpdatedAt = DateTime.UtcNow;
        _es.Update(entity);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteIntegrationAsync(int customerId, Guid uid)
    {
        var entity = await _es.GetByUidAsync(uid);
        if (entity == null || entity.CustomerId != customerId)
            return (false, "Entegrasyon bulunamadi.");

        _es.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<EmailSendResult> TestSendAsync(int customerId, Guid uid, EmailTestSendRequest dto)
    {
        var entity = await _es.GetByUidAsync(uid);
        if (entity == null || entity.CustomerId != customerId)
            return new EmailSendResult { Success = false, Error = "Entegrasyon bulunamadi." };

        var credsJson = _aes.Decrypt(entity.EncryptedCredentials);
        var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson) ?? new();

        var result = await _emailSend.SendWithCredentialsAsync(
            entity.ProviderTypeId, creds, entity.SenderEmail, entity.SenderName,
            dto.ToAddress, "CorpLynk - E-posta Baglanti Testi",
            "<p>Bu bir test e-postasıdır. E-posta entegrasyonunuz başarıyla çalışıyor.</p>");

        entity.LastTestedAt = DateTime.UtcNow;
        entity.LastTestSuccess = result.Success;
        entity.LastTestError = result.Error;
        entity.UpdatedAt = DateTime.UtcNow;
        _es.Update(entity);
        await _uow.SaveChangesAsync();

        return result;
    }

    private async Task ClearDefaultAsync(int customerId)
    {
        var current = await _es.GetDefaultAsync(customerId);
        if (current != null)
        {
            current.IsDefault = false;
            _es.Update(current);
        }
    }

    private static EmailIntegrationListDto MapToListDto(CustomerEmailIntegration e)
    {
        var dto = new EmailIntegrationListDto();
        CopyListFields(e, dto);
        return dto;
    }

    private static void CopyListFields(CustomerEmailIntegration e, EmailIntegrationListDto dto)
    {
        var provider = EmailProviders.GetById(e.ProviderTypeId);
        dto.Id = e.Id;
        dto.Uid = e.Uid;
        dto.ProviderTypeId = e.ProviderTypeId;
        dto.ProviderName = provider?.Description ?? "?";
        dto.ProviderIcon = provider?.Icon ?? "bi-envelope";
        dto.ProviderCss = provider?.CssClass ?? "bg-secondary";
        dto.DisplayName = e.DisplayName;
        dto.SenderEmail = e.SenderEmail;
        dto.SenderName = e.SenderName;
        dto.IsActive = e.IsActive;
        dto.IsDefault = e.IsDefault;
        dto.LastTestedAt = e.LastTestedAt;
        dto.LastTestSuccess = e.LastTestSuccess;
        dto.CreatedAt = e.CreatedAt;
    }
}
