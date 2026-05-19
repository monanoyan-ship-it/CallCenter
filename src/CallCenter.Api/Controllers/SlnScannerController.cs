using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-scanner")]
public class SlnScannerController : ControllerBase
{
    private readonly ISlnScannerFactory _scannerFactory;

    public SlnScannerController(ISlnScannerFactory scannerFactory)
    {
        _scannerFactory = scannerFactory;
    }

    [HttpPost("public/resolve")]
    [AllowAnonymous]
    public async Task<ActionResult<SlnScanResolveDto>> ResolvePublic([FromBody] SlnScanResolveRequest request)
    {
        var result = await _scannerFactory.ResolvePublicAsync(request);
        return result.Found ? Ok(result) : NotFound(result);
    }

    [HttpPost("resolve")]
    [Authorize]
    public async Task<ActionResult<SlnScanResolveDto>> ResolveSalon([FromBody] SlnScanResolveRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var result = await _scannerFactory.ResolveSalonAsync(request, customerId, GetBranchId());
        if (result.Found) return Ok(result);
        if (result.ScanType == "productBarcodeAmbiguous") return Conflict(result);
        return NotFound(result);
    }

    [HttpPost("tokens")]
    [Authorize]
    public async Task<ActionResult<SlnScanTokenDto>> CreateToken([FromBody] SlnScanTokenCreateRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var token = await _scannerFactory.CreateTokenAsync(request, customerId, GetBranchId(), IsSalonOwner());
            return Ok(token);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSalonOwner()
    {
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }
}
