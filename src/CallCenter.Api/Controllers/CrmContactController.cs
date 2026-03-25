using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrmContactController : ControllerBase
{
    private readonly ICrmContactFactory _contactFactory;

    public CrmContactController(ICrmContactFactory contactFactory) => _contactFactory = contactFactory;

    [HttpGet]
    public async Task<ActionResult<List<CrmContactSimpleDto>>> GetContacts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        // Sistem adminleri musteri rehberini goremez
        if (IsSystemAdmin)
            return Ok(new List<CrmContactSimpleDto>());

        var customerId = GetCustomerId();
        var contacts = await _contactFactory.GetContactsAsync(userId.Value, customerId, search, page, pageSize);
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CrmContactSimpleDto>> GetContact(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (IsSystemAdmin)
            return Forbid();

        var contact = await _contactFactory.GetContactAsync(id, userId.Value);
        return contact != null ? Ok(contact) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CrmContactSimpleDto>> CreateContact([FromBody] CreateContactRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var customerId = GetCustomerId();

        var contact = await _contactFactory.CreateContactAsync(req, userId.Value, customerId);
        return Ok(contact);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateContact(int id, [FromBody] UpdateContactRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (success, error) = await _contactFactory.UpdateContactAsync(id, req, userId.Value);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteContact(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (success, error) = await _contactFactory.DeleteContactAsync(id, userId.Value);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/favorite")]
    public async Task<ActionResult> ToggleFavorite(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (success, error) = await _contactFactory.ToggleFavoriteAsync(id, userId.Value);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>CSV dosyasindan rehber kayitlari icerir.</summary>
    [HttpPost("import/csv")]
    public async Task<ActionResult<CsvImportResult>> ImportCsv([FromBody] CsvImportRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var customerId = GetCustomerId();

        var result = await _contactFactory.ImportFromCsvAsync(req, userId.Value, customerId);
        return Ok(result);
    }

    /// <summary>Dosya parse (Excel, Word) → satirlari doner.</summary>
    [HttpPost("import/parse-file")]
    public ActionResult<FileParseResult> ParseFile([FromBody] FileParseRequest req)
    {
        var result = _contactFactory.ParseFile(req);
        return Ok(result);
    }

    /// <summary>Toplu rehber ekleme.</summary>
    [HttpPost("import/batch")]
    public async Task<ActionResult<BulkImportResult>> BulkImport([FromBody] BulkImportRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var customerId = GetCustomerId();

        var result = await _contactFactory.BulkImportAsync(req, userId.Value, customerId);
        return Ok(result);
    }

    /// <summary>LDAP/Active Directory senkronizasyonu (Admin only).</summary>
    [HttpPost("sync/ldap")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LdapSyncResult>> SyncLdap([FromBody] LdapConfigDto config)
    {
        var customerId = GetCustomerId();
        var result = await _contactFactory.SyncFromLdapAsync(config, customerId);
        return Ok(result);
    }

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}
