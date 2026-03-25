using System.Net.Http;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.Services;

/// <summary>
/// API-first rehber servisi.
/// READ: Her zaman API'den okur.
/// WRITE: API'ye yazar, basarisiz olursa lokal buffer'a kaydeder.
/// BackgroundSyncService buffer'daki kayitlari periyodik olarak tekrar dener.
/// </summary>
public class CrmContactService
{
    private readonly HttpClient _http;
    private readonly ILocalRepository _localRepo;

    public CrmContactService(HttpClient http, ILocalRepository localRepo)
    {
        _http = http;
        _localRepo = localRepo;
    }

    public async Task<List<CrmContactSimpleDto>> GetAllAsync(string? search = null)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(search)
                ? "api/contact?pageSize=1000"
                : $"api/contact?search={Uri.EscapeDataString(search)}&pageSize=1000";

            var result = await _http.GetFromJsonAsync<List<CrmContactSimpleDto>>(url);
            return result ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<CrmContactSimpleDto>> GetFavoritesAsync()
    {
        var all = await GetAllAsync();
        return all.Where(c => c.IsFavorite).ToList();
    }

    public async Task<CrmContactSimpleDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CrmContactSimpleDto>($"api/contact/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CreateAsync(string fullName, string phoneNumber, string? company, string? email)
    {
        var request = new CreateContactRequest
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Company = company,
            Email = email
        };

        try
        {
            var response = await _http.PostAsJsonAsync("api/contact", request);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch { }

        // API basarisiz — lokal buffer'a kaydet
        await _localRepo.SaveContactAsync(new LocalContact
        {
            Operation = "Create",
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Company = company,
            Email = email
        });
        return false;
    }

    public async Task<bool> UpdateAsync(int id, string fullName, string phoneNumber, string? company, string? email, bool isFavorite)
    {
        var request = new UpdateContactRequest
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Company = company,
            Email = email,
            IsFavorite = isFavorite
        };

        try
        {
            var response = await _http.PutAsJsonAsync($"api/contact/{id}", request);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch { }

        // API basarisiz — lokal buffer'a kaydet
        await _localRepo.SaveContactAsync(new LocalContact
        {
            Operation = "Update",
            BackendContactId = id,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Company = company,
            Email = email,
            IsFavorite = isFavorite
        });
        return false;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/contact/{id}");
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch { }

        // API basarisiz — lokal buffer'a kaydet
        await _localRepo.SaveContactAsync(new LocalContact
        {
            Operation = "Delete",
            BackendContactId = id
        });
        return false;
    }

    public async Task<bool> ToggleFavoriteAsync(int id)
    {
        try
        {
            var response = await _http.PostAsync($"api/contact/{id}/favorite", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
