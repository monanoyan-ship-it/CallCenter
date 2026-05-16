using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-products")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnProducts)]
public class SlnProductController : ControllerBase
{
    private readonly ISlnProductFactory _productFactory;
    private const string BranchTargetRequiredMessage = "Sube secin veya Tum Subeler secenegini secin";
    private const string ConcreteBranchRequiredMessage = "Bu islem icin sube secilmelidir";
    private const string AllBranchesInitialStockMessage = "Tum Subeler seciliyken baslangic stogu girilemez; stok miktarini sube bazli alis veya sayim ile girin";

    public SlnProductController(ISlnProductFactory productFactory) => _productFactory = productFactory;

    // ═══ Urunler ═══

    [HttpGet]
    public async Task<ActionResult<List<SlnProductDto>>> GetProducts([FromQuery] int? categoryId, [FromQuery] string? search, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var products = await _productFactory.GetProductsAsync(customerId, categoryId, search, ResolveBranchId(branchId));
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnProductDto>> GetProduct(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var product = await _productFactory.GetProductAsync(id, customerId, ResolveBranchId(branchId));
        return product != null ? Ok(product) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnProductDto>> CreateProduct([FromBody] SlnProductCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var validationError = ValidateProduct(dto, target.BranchId);
        if (validationError != null) return BadRequest(validationError);

        var product = await _productFactory.CreateProductAsync(dto, customerId, target.BranchId);
        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(int id, [FromBody] SlnProductUpdateRequest req, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var dto = new SlnProductCreateDto
        {
            CategoryId = req.CategoryId,
            BrandId = req.BrandId,
            Name = req.Name,
            Barcode = req.Barcode,
            PurchasePrice = req.PurchasePrice,
            SalePrice = req.SalePrice,
            StockQuantity = req.StockQuantity,
            MinStockLevel = req.MinStockLevel,
            Unit = req.Unit
        };

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var validationError = ValidateProduct(dto, target.BranchId);
        if (validationError != null) return BadRequest(validationError);

        var (success, error) = await _productFactory.UpdateProductAsync(id, dto, req.IsActive, customerId, target.BranchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.DeleteProductAsync(id, customerId, ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Kategoriler ═══

    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var categories = await _productFactory.GetCategoriesAsync(customerId);
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<ActionResult> CreateCategory([FromBody] SlnNameSortOrderRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var category = await _productFactory.CreateCategoryAsync(req.Name, req.SortOrder, customerId);
        return Ok(category);
    }

    [HttpPut("categories/{id}")]
    public async Task<ActionResult> UpdateCategory(int id, [FromBody] SlnNameSortOrderRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.UpdateCategoryAsync(id, req.Name, req.SortOrder, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("categories/{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.DeleteCategoryAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Markalar ═══

    [HttpGet("brands")]
    public async Task<ActionResult> GetBrands()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var brands = await _productFactory.GetBrandsAsync(customerId);
        return Ok(brands);
    }

    [HttpPost("brands")]
    public async Task<ActionResult> CreateBrand([FromBody] SlnNameRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Marka adi zorunludur");

        var brand = await _productFactory.CreateBrandAsync(req.Name, customerId);
        return Ok(brand);
    }

    [HttpPut("brands/{id}")]
    public async Task<ActionResult> UpdateBrand(int id, [FromBody] SlnNameRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Marka adi zorunludur");

        var (success, error) = await _productFactory.UpdateBrandAsync(id, req.Name, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("brands/{id}")]
    public async Task<ActionResult> DeleteBrand(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.DeleteBrandAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Tedarikciler ═══

    [HttpGet("suppliers")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult<List<SlnSupplierDto>>> GetSuppliers()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var suppliers = await _productFactory.GetSuppliersAsync(customerId);
        return Ok(suppliers);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<SlnLowStockProductDto>>> GetLowStockProducts([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _productFactory.GetLowStockProductsAsync(customerId, ResolveBranchId(branchId)));
    }

    [HttpGet("supplier-orders")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult<List<SlnSupplierOrderDto>>> GetSupplierOrders([FromQuery] int? statusId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _productFactory.GetSupplierOrdersAsync(customerId, statusId));
    }

    [HttpPost("supplier-orders")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult<SlnSupplierOrderDto>> CreateSupplierOrder([FromBody] SlnSupplierOrderCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error, order) = await _productFactory.CreateSupplierOrderAsync(dto, userId, customerId);
        return success && order != null ? Ok(order) : BadRequest(error);
    }

    [HttpPut("supplier-orders/{id}/status")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> UpdateSupplierOrderStatus(int id, [FromBody] SlnSupplierOrderStatusUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.UpdateSupplierOrderStatusAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("suppliers")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult<SlnSupplierDto>> CreateSupplier([FromBody] SlnSupplierCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var supplier = await _productFactory.CreateSupplierAsync(dto, customerId);
        return Ok(supplier);
    }

    [HttpPut("suppliers/{id}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> UpdateSupplier(int id, [FromBody] SlnSupplierUpdateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var dto = new SlnSupplierCreateDto
        {
            Name = req.Name,
            ContactPerson = req.ContactPerson,
            Phone = req.Phone,
            Email = req.Email,
            Address = req.Address,
            TaxNumber = req.TaxNumber,
            Notes = req.Notes
        };

        var (success, error) = await _productFactory.UpdateSupplierAsync(id, dto, req.IsActive, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("suppliers/{id}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> DeleteSupplier(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _productFactory.DeleteSupplierAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Stok Hareket ═══

    [HttpPost("{id}/stock-movements")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> AddStockMovement(int id, [FromBody] SlnStockMovementRequest req, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveConcreteBranchTarget(req.BranchId ?? branchId, ConcreteBranchRequiredMessage);
        if (target.Error != null) return target.Error;

        var (success, error) = await _productFactory.AddStockMovementAsync(
            id, req.MovementTypeId, req.Quantity, req.UnitPrice, req.SupplierId, req.Notes, userId, customerId, target.BranchId);

        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/stock-transfer")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> TransferStock(int id, [FromBody] SlnStockTransferRequest req, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveConcreteBranchTarget(req.FromBranchId ?? branchId, ConcreteBranchRequiredMessage);
        if (target.Error != null) return target.Error;

        var (success, error) = await _productFactory.TransferStockAsync(
            id, target.BranchId, req.ToBranchId, req.Quantity, req.Notes, userId, customerId);

        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/stock-count")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnSuppliers, SalonPortalModules.Ids.SlnExpenses)]
    public async Task<ActionResult> AdjustStockCount(int id, [FromBody] SlnStockCountRequest req, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveConcreteBranchTarget(req.BranchId ?? branchId, ConcreteBranchRequiredMessage);
        if (target.Error != null) return target.Error;

        var (success, error) = await _productFactory.AdjustStockCountAsync(
            id, target.BranchId, req.CountedQuantity, req.Notes, userId, customerId);

        return success ? Ok() : BadRequest(error);
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

    private int? ResolveBranchId(int? requestedBranchId)
        => GetBranchId() ?? requestedBranchId;

    private (int? BranchId, ActionResult? Error) ResolveMutationBranchTarget(int? requestedBranchId, bool allBranches)
    {
        var claimBranchId = GetBranchId();
        if (claimBranchId.HasValue) return (claimBranchId.Value, null);
        if (allBranches) return (null, null);
        if (requestedBranchId.HasValue && requestedBranchId.Value > 0) return (requestedBranchId.Value, null);
        return (null, BadRequest(BranchTargetRequiredMessage));
    }

    private (int? BranchId, ActionResult? Error) ResolveConcreteBranchTarget(int? requestedBranchId, string message)
    {
        var branchId = ResolveBranchId(requestedBranchId);
        if (branchId.HasValue && branchId.Value > 0) return (branchId.Value, null);
        return (null, BadRequest(message));
    }

    private static string? ValidateProduct(SlnProductCreateDto dto, int? branchId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Urun adi zorunludur";
        if (dto.CategoryId <= 0) return "Kategori zorunludur";
        if (!branchId.HasValue)
        {
            if (dto.StockQuantity != 0) return AllBranchesInitialStockMessage;
        }
        else if (dto.StockQuantity <= 0) return "Stok miktari 0'dan buyuk olmalidir";
        if (dto.PurchasePrice <= 0) return "Alis fiyati 0'dan buyuk olmalidir";
        if (dto.SalePrice <= 0) return "Satis fiyati 0'dan buyuk olmalidir";
        return null;
    }
}

// Request modelleri
public class SlnProductUpdateRequest : SlnProductCreateDto
{
    public bool IsActive { get; set; } = true;
}

public class SlnSupplierUpdateRequest : SlnSupplierCreateDto
{
    public bool IsActive { get; set; } = true;
}

public class SlnStockMovementRequest
{
    public int MovementTypeId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? SupplierId { get; set; }
    public int? BranchId { get; set; }
    public string? Notes { get; set; }
}

public class SlnStockTransferRequest
{
    public int? FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class SlnStockCountRequest
{
    public int? BranchId { get; set; }
    public decimal CountedQuantity { get; set; }
    public string? Notes { get; set; }
}

public class SlnNameRequest
{
    public string Name { get; set; } = string.Empty;
}

public class SlnNameSortOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
