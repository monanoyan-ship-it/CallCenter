using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnProductFactory : ISlnProductFactory
{
    private readonly ISlnProductEntityService _products;
    private readonly ISlnProductCategoryEntityService _categories;
    private readonly ISlnProductBrandEntityService _brands;
    private readonly ISlnSupplierEntityService _suppliers;
    private readonly ISlnStockMovementEntityService _stockMovements;
    private readonly ISlnSupplierTransactionEntityService _supplierTransactions;
    private readonly ISlnSupplierOrderEntityService _supplierOrders;
    private readonly ISlnBranchEntityService _branches;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnProductFactory> _logger;

    public SlnProductFactory(
        ISlnProductEntityService products,
        ISlnProductCategoryEntityService categories,
        ISlnProductBrandEntityService brands,
        ISlnSupplierEntityService suppliers,
        ISlnStockMovementEntityService stockMovements,
        ISlnSupplierTransactionEntityService supplierTransactions,
        ISlnSupplierOrderEntityService supplierOrders,
        ISlnBranchEntityService branches,
        IUnitOfWork uow,
        ILogger<SlnProductFactory> logger)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
        _suppliers = suppliers;
        _stockMovements = stockMovements;
        _supplierTransactions = supplierTransactions;
        _supplierOrders = supplierOrders;
        _branches = branches;
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

    public async Task<List<SlnLowStockProductDto>> GetLowStockProductsAsync(int customerId)
    {
        var products = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Where(p => p.CustomerId == customerId && p.IsActive && p.MinStockLevel > 0 && p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(p => new SlnLowStockProductDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            CategoryName = p.Category?.Name,
            StockQuantity = p.StockQuantity,
            MinStockLevel = p.MinStockLevel,
            SuggestedOrderQuantity = CalculateSuggestedOrderQuantity(p),
            PurchasePrice = p.PurchasePrice,
            Unit = p.Unit
        }).ToList();
    }

    public async Task<List<SlnSupplierOrderDto>> GetSupplierOrdersAsync(int customerId, int? statusId = null)
    {
        var query = _supplierOrders.GetAllQueryable()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId);

        if (statusId.HasValue)
            query = query.Where(o => o.StatusId == statusId.Value);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Take(100)
            .ToListAsync();

        return orders.Select(MapSupplierOrderToDto).ToList();
    }

    public async Task<(bool Success, string? Error, SlnSupplierOrderDto? Order)> CreateSupplierOrderAsync(SlnSupplierOrderCreateDto dto, int userId, int customerId)
    {
        if (dto.SupplierId <= 0) return (false, "Tedarikci secilmelidir", null);
        if (dto.Items.Count == 0) return (false, "En az bir urun eklenmelidir", null);

        var supplier = await _suppliers.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.CustomerId == customerId && s.IsActive);
        if (supplier == null) return (false, "Tedarikci bulunamadi", null);

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        var order = new SlnSupplierOrder
        {
            CustomerId = customerId,
            SupplierId = supplier.Id,
            OrderNo = await BuildOrderNoAsync(customerId),
            StatusId = SalonSupplierOrderStatuses.Ids.Draft,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = dto.ExpectedDate,
            Notes = dto.Notes,
            CreatedByPersonnelId = userId > 0 ? userId : null
        };

        foreach (var item in dto.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return (false, "Urun bulunamadi", null);
            if (item.Quantity <= 0)
                return (false, "Siparis miktari 0'dan buyuk olmalidir", null);

            order.Items.Add(new SlnSupplierOrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice ?? product.PurchasePrice,
                Notes = item.Notes
            });
        }

        _supplierOrders.Add(order);
        await _uow.SaveChangesAsync();

        var created = await _supplierOrders.GetAllQueryable()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == order.Id);

        return (true, null, MapSupplierOrderToDto(created));
    }

    public async Task<(bool Success, string? Error)> UpdateSupplierOrderStatusAsync(int orderId, SlnSupplierOrderStatusUpdateDto dto, int customerId)
    {
        if (SalonSupplierOrderStatuses.GetById(dto.StatusId) == null)
            return (false, "Gecersiz siparis durumu");

        var order = await _supplierOrders.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        if (order == null) return (false, "Siparis bulunamadi");

        order.StatusId = dto.StatusId;
        order.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? dto.Notes.Trim() : $"{order.Notes}\n{dto.Notes.Trim()}";
        if (dto.StatusId == SalonSupplierOrderStatuses.Ids.Received)
            order.ReceivedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddStockMovementAsync(int productId, int movementTypeId, decimal quantity, decimal unitPrice, int? supplierId, string? notes, int userId, int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);

        if (product == null) return (false, "Urun bulunamadi");
        if (movementTypeId < 1 || movementTypeId > 5) return (false, "Gecersiz stok hareket tipi");
        if (quantity <= 0) return (false, "Miktar 0'dan buyuk olmali");
        if (unitPrice < 0) return (false, "Birim fiyat negatif olamaz");
        if (movementTypeId == 1 && !supplierId.HasValue) return (false, "Alis hareketi icin tedarikci secilmelidir");
        if (movementTypeId == 1 && unitPrice <= 0) return (false, "Alis hareketi icin birim fiyat 0'dan buyuk olmali");

        SlnSupplier? supplier = null;
        if (supplierId.HasValue)
        {
            supplier = await _suppliers.GetAllQueryable()
                .FirstOrDefaultAsync(s => s.Id == supplierId.Value && s.CustomerId == customerId);

            if (supplier == null) return (false, "Tedarikci bulunamadi");
        }

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

        if (movementTypeId == 1 && supplier != null)
        {
            var amount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
            _supplierTransactions.Add(new SlnSupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionTypeId = 1,
                Amount = amount,
                Description = BuildPurchaseDescription(product, quantity, unitPrice, notes),
                TransactionDate = DateTime.UtcNow
            });
        }

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

    public async Task<(bool Success, string? Error)> TransferStockAsync(
        int productId,
        int? fromBranchId,
        int toBranchId,
        decimal quantity,
        string? notes,
        int userId,
        int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);
        if (product == null) return (false, "Urun bulunamadi");
        if (quantity <= 0) return (false, "Transfer miktari 0'dan buyuk olmali");
        if (product.StockQuantity < quantity) return (false, "Transfer icin yeterli global stok yok");

        if (fromBranchId.HasValue && fromBranchId.Value == toBranchId)
            return (false, "Kaynak ve hedef sube ayni olamaz");

        if (fromBranchId.HasValue && !await BranchExistsAsync(fromBranchId.Value, customerId))
            return (false, "Kaynak sube bulunamadi");
        if (!await BranchExistsAsync(toBranchId, customerId))
            return (false, "Hedef sube bulunamadi");

        var unitPrice = product.PurchasePrice;
        var transferUid = Guid.NewGuid().ToString("N");
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : " - " + notes.Trim();

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = fromBranchId,
            MovementTypeId = 4,
            Quantity = -quantity,
            UnitPrice = unitPrice,
            Notes = $"TransferOut:{transferUid}|ToBranch:{toBranchId}{cleanNotes}",
            CreatedByPersonnelId = userId
        });

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = toBranchId,
            MovementTypeId = 4,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Notes = $"TransferIn:{transferUid}|FromBranch:{fromBranchId?.ToString() ?? "Merkez"}{cleanNotes}",
            CreatedByPersonnelId = userId
        });

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Stok transferi audit kaydi olustu: Product={ProductId}, From={FromBranchId}, To={ToBranchId}, Qty={Qty}",
            productId, fromBranchId, toBranchId, quantity);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AdjustStockCountAsync(
        int productId,
        int? branchId,
        decimal countedQuantity,
        string? notes,
        int userId,
        int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);
        if (product == null) return (false, "Urun bulunamadi");
        if (countedQuantity < 0) return (false, "Sayilan stok negatif olamaz");
        if (branchId.HasValue && !await BranchExistsAsync(branchId.Value, customerId))
            return (false, "Sube bulunamadi");

        var before = product.StockQuantity;
        var difference = countedQuantity - before;
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : " - " + notes.Trim();

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = branchId,
            MovementTypeId = 6,
            Quantity = difference,
            UnitPrice = product.PurchasePrice,
            Notes = $"StockCount|Before:{before:0.##}|Counted:{countedQuantity:0.##}|Diff:{difference:0.##}{cleanNotes}",
            CreatedByPersonnelId = userId
        });

        product.StockQuantity = countedQuantity;
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Stok sayim farki kaydedildi: Product={ProductId}, Branch={BranchId}, Before={Before}, Counted={Counted}, Diff={Diff}",
            productId, branchId, before, countedQuantity, difference);
        return (true, null);
    }

    private async Task<bool> BranchExistsAsync(int branchId, int customerId)
        => await _branches.GetAllQueryable().AnyAsync(b => b.Id == branchId && b.CustomerId == customerId && b.IsActive);

    private static string BuildPurchaseDescription(SlnProduct product, decimal quantity, decimal unitPrice, string? notes)
    {
        var description = $"Alis kaydi: {product.Name} ({quantity:0.##} {product.Unit} x {unitPrice:0.##} TL)";
        return string.IsNullOrWhiteSpace(notes) ? description : $"{description} - {notes.Trim()}";
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
        IsActive = p.IsActive,
        IsLowStock = p.MinStockLevel > 0 && p.StockQuantity <= p.MinStockLevel,
        SuggestedOrderQuantity = CalculateSuggestedOrderQuantity(p)
    };

    private async Task<string> BuildOrderNoAsync(int customerId)
    {
        var prefix = $"SO-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _supplierOrders.GetAllQueryable()
            .CountAsync(o => o.CustomerId == customerId && o.OrderNo.StartsWith(prefix));
        return $"{prefix}{count + 1:000}";
    }

    private static decimal CalculateSuggestedOrderQuantity(SlnProduct p)
    {
        if (p.MinStockLevel <= 0) return 0;
        var target = p.MinStockLevel * 2;
        var needed = target - p.StockQuantity;
        return needed > 0 ? needed : 0;
    }

    private static SlnSupplierOrderDto MapSupplierOrderToDto(SlnSupplierOrder order) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        SupplierId = order.SupplierId,
        SupplierName = order.Supplier?.Name ?? "",
        StatusId = order.StatusId,
        StatusName = SalonSupplierOrderStatuses.GetById(order.StatusId)?.Description ?? order.StatusId.ToString(),
        OrderDate = order.OrderDate,
        ExpectedDate = order.ExpectedDate,
        ReceivedAt = order.ReceivedAt,
        Notes = order.Notes,
        TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice),
        Items = order.Items.Select(i => new SlnSupplierOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            ReceivedQuantity = i.ReceivedQuantity,
            Unit = i.Product?.Unit ?? ""
        }).ToList()
    };
}
