using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            Price = req.Price
        };

        var (success, error) = await _serviceFactory.UpdateServiceAsync(id, dto, req.IsActive, customerId);
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
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
