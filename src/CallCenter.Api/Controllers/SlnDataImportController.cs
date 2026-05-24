using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-data-import")]
[Authorize(Roles = "Admin,CustomerUser")]
public class SlnDataImportController : ControllerBase
{
    private readonly ISlnDataImportFactory _dataImportFactory;

    public SlnDataImportController(ISlnDataImportFactory dataImportFactory)
    {
        _dataImportFactory = dataImportFactory;
    }

    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        if (!CanImport()) return Forbid();
        return Ok(_dataImportFactory.GetTemplates());
    }

    [HttpGet("template/{importType}")]
    public IActionResult DownloadTemplate(string importType)
    {
        if (!CanImport()) return Forbid();

        var bytes = _dataImportFactory.BuildExcelTemplate(importType, out var fileName);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("preview")]
    [RequestSizeLimit(20_971_520)]
    public async Task<IActionResult> Preview(IFormFile file, [FromForm] string importType, [FromForm] int? branchId)
    {
        if (!CanImport()) return Forbid();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "Dosya secilmedi." });
        if (!IsSupportedFile(file.FileName)) return BadRequest(new { message = "Sadece Excel XLSX dosyalari desteklenir. Eski XLS dosyalarini Excel'de XLSX olarak kaydedin." });

        await using var stream = file.OpenReadStream();
        var result = await _dataImportFactory.PreviewAsync(importType, stream, file.FileName, customerId, GetBranchId() ?? branchId);
        return Ok(result);
    }

    [HttpPost("commit")]
    [RequestSizeLimit(20_971_520)]
    public async Task<IActionResult> Commit(IFormFile file, [FromForm] string importType, [FromForm] int? branchId)
    {
        if (!CanImport()) return Forbid();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "Dosya secilmedi." });
        if (!IsSupportedFile(file.FileName)) return BadRequest(new { message = "Sadece Excel XLSX dosyalari desteklenir. Eski XLS dosyalarini Excel'de XLSX olarak kaydedin." });

        await using var stream = file.OpenReadStream();
        var result = await _dataImportFactory.ImportAsync(importType, stream, file.FileName, customerId, GetUserId(), GetBranchId() ?? branchId);
        return Ok(result);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    private static bool IsSupportedFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".xlsx" or ".xlsm" or ".xltx";
    }

    private bool CanImport()
    {
        if (User.IsInRole("Admin")) return true;
        if (User.FindFirst("IsCustomerAdmin")?.Value == "true") return true;
        var roleClaim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(roleClaim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }
}
