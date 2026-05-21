using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-services")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnServices)]
public class SlnServiceController : ControllerBase
{
    private readonly ISlnServiceFactory _serviceFactory;

    public SlnServiceController(ISlnServiceFactory serviceFactory) => _serviceFactory = serviceFactory;

    /// <summary>Kategoriler ve altindaki hizmetler (agac yapisi)</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<SlnServiceCategoryDto>>> GetCategories()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var categories = await _serviceFactory.GetCategoriesWithServicesAsync(customerId);
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<SlnServiceCategoryDto>> CreateCategory([FromBody] SlnServiceCategoryCreateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var category = await _serviceFactory.CreateCategoryAsync(req.Name, req.SortOrder, customerId);
        return Ok(category);
    }

    [HttpPut("categories/{id}")]
    public async Task<ActionResult> UpdateCategory(int id, [FromBody] SlnServiceCategoryUpdateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _serviceFactory.UpdateCategoryAsync(id, req.Name, req.SortOrder, req.IsActive, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("categories/{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _serviceFactory.DeleteCategoryAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Hizmet listesi (opsiyonel kategori filtresi)</summary>
    [HttpGet]
    public async Task<ActionResult<List<SlnServiceDto>>> GetServices([FromQuery] int? categoryId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var services = await _serviceFactory.GetServicesAsync(customerId, categoryId);
        return Ok(services);
    }

    [HttpPost]
    public async Task<ActionResult<SlnServiceDto>> CreateService([FromBody] SlnServiceCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var service = await _serviceFactory.CreateServiceAsync(dto, customerId);
        return Ok(service);
    }

    [HttpGet("resources")]
    public async Task<ActionResult<List<SlnResourceDto>>> GetResources()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveServiceBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        return Ok(await _serviceFactory.GetResourcesAsync(customerId, access.BranchScopeId));
    }

    [HttpPost("resources")]
    public async Task<ActionResult<SlnResourceDto>> CreateResource([FromBody] SlnResourceCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveServiceBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        return Ok(await _serviceFactory.CreateResourceAsync(dto, customerId, access.BranchScopeId));
    }

    [HttpPut("resources/{id}")]
    public async Task<ActionResult> UpdateResource(int id, [FromBody] SlnResourceCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveServiceBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var (success, error) = await _serviceFactory.UpdateResourceAsync(id, dto, customerId, access.BranchScopeId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("resources/{id}")]
    public async Task<ActionResult> DeleteResource(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveServiceBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;

        var (success, error) = await _serviceFactory.DeleteResourceAsync(id, customerId, access.BranchScopeId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("combos")]
    public async Task<ActionResult<List<SlnServiceComboDto>>> GetCombos()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _serviceFactory.GetCombosAsync(customerId));
    }

    [HttpPost("combos")]
    public async Task<ActionResult<SlnServiceComboDto>> CreateCombo([FromBody] SlnServiceComboCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            return Ok(await _serviceFactory.CreateComboAsync(dto, customerId));
        }
        catch (DbUpdateException ex) when (IsDuplicateComboName(ex))
        {
            return BadRequest("Ayni isimde bir combo zaten var");
        }
    }

    [HttpPut("combos/{id}")]
    public async Task<ActionResult> UpdateCombo(int id, [FromBody] SlnServiceComboCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var (success, error) = await _serviceFactory.UpdateComboAsync(id, dto, customerId);
            return success ? Ok() : BadRequest(error);
        }
        catch (DbUpdateException ex) when (IsDuplicateComboName(ex))
        {
            return BadRequest("Ayni isimde bir combo zaten var");
        }
    }

    [HttpDelete("combos/{id}")]
    public async Task<ActionResult> DeleteCombo(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _serviceFactory.DeleteComboAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateService(int id, [FromBody] SlnServiceUpdateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var dto = new SlnServiceCreateDto
        {
            CategoryId = req.CategoryId,
            Name = req.Name,
            DurationMinutes = req.DurationMinutes,
            BufferBeforeMinutes = req.BufferBeforeMinutes,
            BufferAfterMinutes = req.BufferAfterMinutes,
            ProcessingMinutes = req.ProcessingMinutes,
            Price = req.Price,
            ParentServiceId = req.ParentServiceId,
            IsAddOn = req.IsAddOn,
            RequiresConsultation = req.RequiresConsultation,
            RequiresPatchTest = req.RequiresPatchTest,
            PrerequisiteNotes = req.PrerequisiteNotes,
            ResourceRequirements = req.ResourceRequirements ?? []
        };

        var (success, error) = await _serviceFactory.UpdateServiceAsync(
            id,
            dto,
            req.IsActive,
            customerId,
            syncResourceRequirements: req.ResourceRequirements != null);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteService(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _serviceFactory.DeleteServiceAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
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
        if (User.IsInRole("Admin")) return true;
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }

    private ServiceBranchAccess ResolveServiceBranchAccess()
    {
        if (IsSalonOwner()) return new(true, null, null);

        var branchId = GetBranchId();
        return branchId.HasValue ? new(true, branchId.Value, null) : new(false, null, Forbid());
    }

    private readonly record struct ServiceBranchAccess(bool IsAllowed, int? BranchScopeId, ActionResult? ErrorResult);

    private static bool IsDuplicateComboName(DbUpdateException ex)
    {
        var message = ex.GetBaseException().Message;
        return message.Contains("IX_SlnServiceCombos_CustomerId_Name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase);
    }
}

// Request modelleri (controller scope)
public class SlnServiceCategoryCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class SlnServiceCategoryUpdateRequest : SlnServiceCategoryCreateRequest
{
    public bool IsActive { get; set; } = true;
}

public class SlnServiceUpdateRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public int ProcessingMinutes { get; set; }
    public decimal Price { get; set; }
    public int? ParentServiceId { get; set; }
    public bool IsAddOn { get; set; }
    public bool RequiresConsultation { get; set; }
    public bool RequiresPatchTest { get; set; }
    public string? PrerequisiteNotes { get; set; }
    public List<SlnServiceResourceRequirementCreateDto>? ResourceRequirements { get; set; }
    public bool? IsActive { get; set; }
}
