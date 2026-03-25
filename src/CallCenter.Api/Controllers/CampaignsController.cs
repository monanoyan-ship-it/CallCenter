using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignsController : AuditableControllerBase
{
    private readonly ICampaignFactory _campaignFactory;

    public CampaignsController(IAuditFactory auditFactory, ICampaignFactory campaignFactory)
        : base(auditFactory)
    {
        _campaignFactory = campaignFactory;
    }

    /// <summary>Kampanya listesi (firma bazli)</summary>
    [HttpGet]
    public async Task<IActionResult> GetCampaigns()
    {
        return Ok(await _campaignFactory.GetCampaignsAsync(GetCustomerId()));
    }

    /// <summary>Kampanya detay + kisiler</summary>
    [HttpGet("{uid:guid}")]
    public async Task<IActionResult> GetCampaign(Guid uid)
    {
        var campaign = await _campaignFactory.GetCampaignByUidAsync(uid, GetCustomerId());
        if (campaign == null) return NotFound();

        // Sistem adminleri bireysel kisi bilgilerini goremez, sadece sayilar
        if (IsSystemAdmin)
            campaign.CrmContacts = new();

        return Ok(campaign);
    }

    /// <summary>Yeni kampanya olustur</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest req)
    {
        var personnelId = GetPersonnelId();
        if (personnelId == null) return Forbid();

        var result = await _campaignFactory.CreateCampaignAsync(req, personnelId.Value, GetCustomerId());

        await AuditCrudAsync("Create", "CallCampaign", result.Uid.ToString(),
            $"Kampanya olusturuldu: {req.Name}");

        return Ok(result);
    }

    /// <summary>Kampanya guncelle</summary>
    [HttpPut("{uid:guid}")]
    public async Task<IActionResult> UpdateCampaign(Guid uid, [FromBody] UpdateCampaignRequest req)
    {
        var (success, error) = await _campaignFactory.UpdateCampaignAsync(uid, req, GetCustomerId());
        if (!success) return BadRequest(error);

        await AuditCrudAsync("Update", "CallCampaign", uid.ToString(),
            $"Kampanya guncellendi");

        return Ok();
    }

    /// <summary>Kampanya sil</summary>
    [HttpDelete("{uid:guid}")]
    public async Task<IActionResult> DeleteCampaign(Guid uid)
    {
        var (success, error) = await _campaignFactory.DeleteCampaignAsync(uid, GetCustomerId());
        if (!success) return BadRequest(error);

        await AuditCrudAsync("Delete", "CallCampaign", uid.ToString(),
            $"Kampanya silindi");

        return Ok();
    }

    /// <summary>Kampanyaya kisi ekle</summary>
    [HttpPost("{uid:guid}/contacts")]
    public async Task<IActionResult> AddContacts(Guid uid, [FromBody] AddCampaignContactsRequest req)
    {
        var (success, error) = await _campaignFactory.AddContactsAsync(uid, req, GetCustomerId());
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Kampanyadan kisi cikar</summary>
    [HttpDelete("{uid:guid}/contacts/{id:int}")]
    public async Task<IActionResult> RemoveContact(Guid uid, int id)
    {
        var (success, error) = await _campaignFactory.RemoveContactAsync(uid, id, GetCustomerId());
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Kisileri operatore ata</summary>
    [HttpPost("{uid:guid}/assign")]
    public async Task<IActionResult> AssignContacts(Guid uid, [FromBody] AssignCampaignContactsRequest req)
    {
        var (success, error) = await _campaignFactory.AssignContactsAsync(uid, req, GetCustomerId());
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Kampanya kisi durumunu guncelle (operator arama sonucu isareti)</summary>
    [HttpPut("{uid:guid}/contacts/{id:int}")]
    public async Task<IActionResult> UpdateContactStatus(Guid uid, int id, [FromBody] UpdateCampaignContactStatusRequest req)
    {
        var (success, error) = await _campaignFactory.UpdateContactStatusAsync(uid, id, req, GetCustomerId());
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Operatorun kendi arama listesi</summary>
    [HttpGet("my-list")]
    public async Task<IActionResult> GetMyCallList()
    {
        var personnelId = GetPersonnelId();
        if (personnelId == null) return Forbid();

        return Ok(await _campaignFactory.GetMyCallListAsync(personnelId.Value));
    }

    /// <summary>Operatorun listesindeki bir kaydi guncelle (kampanya uid gerektirmez)</summary>
    [HttpPut("my-list/{id:int}")]
    public async Task<IActionResult> UpdateMyListItem(int id, [FromBody] UpdateCampaignContactStatusRequest req)
    {
        var personnelId = GetPersonnelId();
        if (personnelId == null) return Forbid();

        var (success, error) = await _campaignFactory.UpdateMyListItemAsync(id, req, personnelId.Value);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Firma operatorlerinin listesi (atama icin)</summary>
    [HttpGet("operators")]
    public async Task<IActionResult> GetOperators()
    {
        return Ok(await _campaignFactory.GetOperatorsAsync(GetCustomerId()));
    }

    // ─── Helpers ───

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int? GetCustomerId()
    {
        var claim = User.FindFirstValue("CustomerId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetPersonnelId()
    {
        var claim = User.FindFirstValue("CustomerPersonnelId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
