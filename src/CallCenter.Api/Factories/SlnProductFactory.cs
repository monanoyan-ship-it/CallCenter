using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnProductFactory : ISlnProductFactory
{
    private readonly ISlnProductEntityService _products;
    private readonly ISlnProductCategoryEntityService _categories;
    private readonly ISlnProductBrandEntityService _brands;
    private readonly ISlnSupplierEntityService _suppliers;
    private readonly ISlnStockMovementEntityService _stockMovements;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnProductFactory> _logger;

    public SlnProductFactory(
        ISlnProductEntityService products,
        ISlnProductCategoryEntityService categories,
        ISlnProductBrandEntityService brands,
        ISlnSupplierEntityService suppliers,
        ISlnStockMovementEntityService stockMovements,
        IUnitOfWork uow,
        ILogger<SlnProductFactory> logger)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
        _suppliers = suppliers;
        _stockMovements = stockMovements;
        _uow = uow;
        _logger = logger;
    }

    // ═══ Urun ═══

    public async Task<List<SlnProductDto>> GetProductsAsync(int customerId, int? categoryId = null, string? search = null)
    {
        var query = _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.Contains(s)));
        }

        var products = await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapProductToDto).ToList();
    }

    public async Task<SlnProductDto?> GetProductAsync(int productId, int customerId)
    {
        var product = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);

        return product != null ? MapProductToDto(product) : null;
    }

    public async Task<SlnProductDto> CreateProductAsync(SlnProductCreateDto dto, int customerId)
    {
        var product = new SlnProduct
        {
            CustomerId = customerId,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Name = dto.Name,
            Barcode = dto.Barcode,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            StockQuantity = dto.StockQuantity,
            MinStockLevel = dto.MinStockLevel,
            Unit = dto.Unit
        };

        _products.Add(product);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni urun olusturuldu: {ProductId} - {Name}", product.Id, product.Name);

        var created = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstAsync(p => p.Id == product.Id);

        return MapProductToDto(created);
    }

    public async Task<(bool Success, string? Error)> UpdateProductAsync(int productId, SlnProductCreateDto dto, bool isActive, int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);

        if (product == null) return (false, "Urun bulunamadi");

        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.Name = dto.Name;
        product.Barcode = dto.Barcode;
        product.PurchasePrice = dto.PurchasePrice;
        product.SalePrice = dto.SalePrice;
        product.StockQuantity = dto.StockQuantity;
        product.MinStockLevel = dto.MinStockLevel;
        product.Unit = dto.Unit;
        product.IsActive = isActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteProductAsync(int productId, int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);

        if (product == null) return (false, "Urun bulunamadi");

        _products.Remove(product);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Kategori ═══

    public async Task<List<object>> GetCategoriesAsync(int customerId)
    {
        var categories = await _categories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return categories.Select(c => (object)new { c.Id, c.Name, c.SortOrder }).ToList();
    }

    public async Task<object> CreateCategoryAsync(string name, int sortOrder, int customerId)
    {
        var category = new SlnProductCategory
        {
            CustomerId = customerId,
            Name = name,
            SortOrder = sortOrder
        };

        _categories.Add(category);
        await _uow.SaveChangesAsync();

        return new { category.Id, category.Name, category.SortOrder };
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, int customerId)
    {
        var category = await _categories.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadi");

        category.Name = name;
        category.SortOrder = sortOrder;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId)
    {
        var category = await _categories.GetAllQueryable()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadi");
        if (category.Products.Any()) return (false, "Kategoride urun bulunuyor, once urunleri silin");

        _categories.Remove(category);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Marka ═══

    public async Task<List<object>> GetBrandsAsync(int customerId)
    {
        var brands = await _brands.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderBy(b => b.Name)
            .ToListAsync();

        return brands.Select(b => (object)new { b.Id, b.Name }).ToList();
    }

    public async Task<object> CreateBrandAsync(string name, int customerId)
    {
        var brand = new SlnProductBrand
        {
            CustomerId = customerId,
            Name = name
        };

        _brands.Add(brand);
        await _uow.SaveChangesAsync();

        return new { brand.Id, brand.Name };
    }

    public async Task<(bool Success, string? Error)> UpdateBrandAsync(int brandId, string name, int customerId)
    {
        var brand = await _brands.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == brandId && b.CustomerId == customerId);

        if (brand == null) return (false, "Marka bulunamadi");

        brand.Name = name;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteBrandAsync(int brandId, int customerId)
    {
        var brand = await _brands.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == brandId && b.CustomerId == customerId);

        if (brand == null) return (false, "Marka bulunamadi");

        _brands.Remove(brand);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Tedarikci ═══

    public async Task<List<SlnSupplierDto>> GetSuppliersAsync(int customerId)
    {
        var suppliers = await _suppliers.GetAllQueryable()
            .Where(s => s.CustomerId == customerId)
            .Include(s => s.Transactions)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return suppliers.Select(s => new SlnSupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Balance = s.Transactions.Sum(t => t.TransactionTypeId == 1 ? t.Amount : -t.Amount),
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<SlnSupplierDto> CreateSupplierAsync(SlnSupplierCreateDto dto, int customerId)
    {
        var supplier = new SlnSupplier
        {
            CustomerId = customerId,
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxNumber = dto.TaxNumber,
            Notes = dto.Notes
        };

        _suppliers.Add(supplier);
        await _uow.SaveChangesAsync();

        return new SlnSupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            IsActive = supplier.IsActive
        };
    }

    public async Task<(bool Success, string? Error)> UpdateSupplierAsync(int supplierId, SlnSupplierCreateDto dto, bool isActive, int customerId)
    {
        var supplier = await _suppliers.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.CustomerId == customerId);

        if (supplier == null) return (false, "Tedarikci bulunamadi");

        supplier.Name = dto.Name;
        supplier.ContactPerson = dto.ContactPerson;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;
        supplier.TaxNumber = dto.TaxNumber;
        supplier.Notes = dto.Notes;
        supplier.IsActive = isActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteSupplierAsync(int supplierId, int customerId)
    {
        var supplier = await _suppliers.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.CustomerId == customerId);

        if (supplier == null) return (false, "Tedarikci bulunamadi");

        _suppliers.Remove(supplier);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Stok Hareket ═══

    public async Task<(bool Success, string? Error)> AddStockMovementAsync(int productId, int movementTypeId, decimal quantity, decimal unitPrice, int? supplierId, string? notes, int userId, int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);

        if (product == null) return (false, "Urun bulunamadi");

        var movement = new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            MovementTypeId = movementTypeId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            SupplierId = supplierId,
            Notes = notes,
            CreatedByPersonnelId = userId
        };

        // Stok miktarini guncelle
        // 1=Purchase(+), 2=Sale(-), 3=InternalUse(-), 4=Transfer(0), 5=Return(+)
        product.StockQuantity += movementTypeId switch
        {
            1 => quantity,   // Alis
            2 => -quantity,  // Satis
            3 => -quantity,  // Dahili kullanim
            5 => quantity,   // Iade
            _ => 0
        };

        _stockMovements.Add(movement);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Stok hareketi eklendi: Product={ProductId}, Type={TypeId}, Qty={Qty}",
            productId, movementTypeId, quantity);

        return (true, null);
    }

    private static SlnProductDto MapProductToDto(SlnProduct p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Barcode = p.Barcode,
        CategoryName = p.Category?.Name ?? "",
        BrandName = p.Brand?.Name,
        PurchasePrice = p.PurchasePrice,
        SalePrice = p.SalePrice,
        StockQuantity = p.StockQuantity,
        MinStockLevel = p.MinStockLevel,
        Unit = p.Unit,
        IsActive = p.IsActive
    };
}
