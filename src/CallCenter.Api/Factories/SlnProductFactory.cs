using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.Interfaces;
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
    private readonly ISlnStockBalanceService _stockBalances;
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
        ISlnStockBalanceService stockBalances,
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
        _stockBalances = stockBalances;
        _uow = uow;
        _logger = logger;
    }

    // ═══ Urun ═══

    public async Task<List<SlnProductDto>> GetProductsAsync(int customerId, int? categoryId = null, string? search = null, int? branchId = null)
    {
        var query = _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(p => p.BranchId == null || p.BranchId == branchId.Value);

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

        var stockMap = await _stockBalances.GetStockQuantitiesAsync(customerId, products.Select(p => p.Id), branchId);
        return products
            .Select(p => MapProductToDto(p, stockMap.GetValueOrDefault(p.Id, ResolveStockFallback(branchId, p.StockQuantity))))
            .ToList();
    }

    public async Task<SlnProductDto?> GetProductAsync(int productId, int customerId, int? branchId = null)
    {
        var product = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value));

        if (product == null) return null;

        var stockQuantity = await _stockBalances.GetStockQuantityAsync(customerId, product.Id, branchId, product.StockQuantity);
        return MapProductToDto(product, stockQuantity);
    }

    public async Task<List<SlnProductDto>> GetProductsByBarcodeAsync(string barcode, int customerId, int? branchId = null)
    {
        var normalized = NormalizeBarcode(barcode);
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        var query = _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                && p.Barcode != null
                && p.Barcode.ToLower() == normalized);

        if (branchId.HasValue)
            query = query.Where(p => p.BranchId == null || p.BranchId == branchId.Value);

        var products = await query
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .OrderByDescending(p => branchId.HasValue && p.BranchId == branchId.Value)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var stockMap = await _stockBalances.GetStockQuantitiesAsync(customerId, products.Select(p => p.Id), branchId);
        return products
            .Select(p => MapProductToDto(p, stockMap.GetValueOrDefault(p.Id, ResolveStockFallback(branchId, p.StockQuantity))))
            .ToList();
    }

    public async Task<SlnProductDto> CreateProductAsync(SlnProductCreateDto dto, int customerId, int? branchId = null)
    {
        var product = new SlnProduct
        {
            CustomerId = customerId,
            BranchId = branchId,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Name = dto.Name,
            Barcode = dto.Barcode,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            StockQuantity = branchId.HasValue ? dto.StockQuantity : 0,
            MinStockLevel = dto.MinStockLevel,
            Unit = dto.Unit
        };

        _products.Add(product);
        await _uow.SaveChangesAsync();
        if (branchId.HasValue)
        {
            await _stockBalances.SetStockQuantityAsync(customerId, product.Id, branchId, dto.StockQuantity);
            await _stockBalances.SyncProductTotalAsync(product, customerId);
            await _uow.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Yeni urun olusturuldu. CustomerId={CustomerId} BranchId={BranchId} ProductId={ProductId} Name={Name} StockQuantity={StockQuantity}",
            customerId, branchId, product.Id, product.Name, dto.StockQuantity);

        var created = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstAsync(p => p.Id == product.Id);

        var stockQuantity = branchId.HasValue
            ? await _stockBalances.GetStockQuantityAsync(customerId, created.Id, branchId, created.StockQuantity)
            : created.StockQuantity;
        return MapProductToDto(created, stockQuantity);
    }

    public async Task<(bool Success, string? Error)> UpdateProductAsync(int productId, SlnProductCreateDto dto, bool isActive, int customerId, int? branchId = null)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value));

        if (product == null) return (false, "Urun bulunamadi");

        product.BranchId = branchId;
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.Name = dto.Name;
        product.Barcode = dto.Barcode;
        product.PurchasePrice = dto.PurchasePrice;
        product.SalePrice = dto.SalePrice;
        product.MinStockLevel = dto.MinStockLevel;
        product.Unit = dto.Unit;
        product.IsActive = isActive;

        if (branchId.HasValue)
        {
            await _stockBalances.SetStockQuantityAsync(customerId, product.Id, branchId, dto.StockQuantity);
            await _stockBalances.SyncProductTotalAsync(product, customerId);
        }

        await _uow.SaveChangesAsync();
        _logger.LogInformation(
            "Urun guncellendi. CustomerId={CustomerId} BranchId={BranchId} ProductId={ProductId} Name={Name} StockQuantity={StockQuantity} IsActive={IsActive}",
            customerId, branchId, product.Id, product.Name, dto.StockQuantity, isActive);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteProductAsync(int productId, int customerId, int? branchId = null)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value));

        if (product == null) return (false, "Urun bulunamadi");

        _products.Remove(product);
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Urun silindi. CustomerId={CustomerId} BranchId={BranchId} ProductId={ProductId} Name={Name}",
            customerId, branchId, productId, product.Name);
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
        _logger.LogInformation("Urun kategorisi olusturuldu. CustomerId={CustomerId} CategoryId={CategoryId} Name={Name}",
            customerId, category.Id, category.Name);

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
        _logger.LogInformation("Urun kategorisi guncellendi. CustomerId={CustomerId} CategoryId={CategoryId} Name={Name}",
            customerId, category.Id, category.Name);
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
        _logger.LogInformation("Urun kategorisi silindi. CustomerId={CustomerId} CategoryId={CategoryId} Name={Name}",
            customerId, categoryId, category.Name);
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
        var normalizedName = name.Trim();
        var normalizedLower = normalizedName.ToLower();
        var existing = await _brands.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.Name.ToLower() == normalizedLower);

        if (existing != null)
        {
            _logger.LogInformation("Urun markasi yeniden kullanildi. CustomerId={CustomerId} BrandId={BrandId} Name={Name}",
                customerId, existing.Id, existing.Name);
            return new { existing.Id, existing.Name };
        }

        var brand = new SlnProductBrand
        {
            CustomerId = customerId,
            Name = normalizedName
        };

        _brands.Add(brand);
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Urun markasi olusturuldu. CustomerId={CustomerId} BrandId={BrandId} Name={Name}",
            customerId, brand.Id, brand.Name);

        return new { brand.Id, brand.Name };
    }

    public async Task<(bool Success, string? Error)> UpdateBrandAsync(int brandId, string name, int customerId)
    {
        var normalizedName = name.Trim();
        var normalizedLower = normalizedName.ToLower();
        var brand = await _brands.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == brandId && b.CustomerId == customerId);

        if (brand == null) return (false, "Marka bulunamadi");

        var exists = await _brands.GetAllQueryable()
            .AnyAsync(b => b.Id != brandId && b.CustomerId == customerId && b.Name.ToLower() == normalizedLower);

        if (exists) return (false, "Bu marka zaten var");

        brand.Name = normalizedName;
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Urun markasi guncellendi. CustomerId={CustomerId} BrandId={BrandId} Name={Name}",
            customerId, brand.Id, brand.Name);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteBrandAsync(int brandId, int customerId)
    {
        var brand = await _brands.GetAllQueryable()
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == brandId && b.CustomerId == customerId);

        if (brand == null) return (false, "Marka bulunamadi");
        if (brand.Products.Any()) return (false, "Bu markaya bagli urun var, once urunleri baska markaya alin");

        _brands.Remove(brand);
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Urun markasi silindi. CustomerId={CustomerId} BrandId={BrandId} Name={Name}",
            customerId, brandId, brand.Name);
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

    public async Task<List<SlnLowStockProductDto>> GetLowStockProductsAsync(int customerId, int? branchId = null)
    {
        var products = await _products.GetAllQueryable()
            .Include(p => p.Category)
            .Where(p => p.CustomerId == customerId && p.IsActive && p.MinStockLevel > 0 &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value))
            .OrderBy(p => p.Name)
            .ToListAsync();

        var stockMap = await _stockBalances.GetStockQuantitiesAsync(customerId, products.Select(p => p.Id), branchId);

        return products
            .Select(p => new { Product = p, StockQuantity = stockMap.GetValueOrDefault(p.Id, ResolveStockFallback(branchId, p.StockQuantity)) })
            .Where(x => x.StockQuantity <= x.Product.MinStockLevel)
            .OrderBy(x => x.StockQuantity)
            .ThenBy(x => x.Product.Name)
            .Select(x => new SlnLowStockProductDto
            {
                ProductId = x.Product.Id,
                ProductName = x.Product.Name,
                CategoryName = x.Product.Category?.Name,
                StockQuantity = x.StockQuantity,
                MinStockLevel = x.Product.MinStockLevel,
                SuggestedOrderQuantity = CalculateSuggestedOrderQuantity(x.Product, x.StockQuantity),
                PurchasePrice = x.Product.PurchasePrice,
                Unit = x.Product.Unit
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

    public async Task<(bool Success, string? Error, SlnSupplierOrderDto? Order)> CreateSupplierOrderAsync(SlnSupplierOrderCreateDto dto, int personnelId, int customerId)
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
            CreatedByPersonnelId = personnelId > 0 ? personnelId : null
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
        _logger.LogInformation(
            "Tedarik siparisi olusturuldu. CustomerId={CustomerId} PersonnelId={PersonnelId} SupplierId={SupplierId} OrderId={OrderId} OrderNo={OrderNo} ItemCount={ItemCount}",
            customerId, personnelId, supplier.Id, order.Id, order.OrderNo, order.Items.Count);

        var created = await _supplierOrders.GetAllQueryable()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == order.Id);

        return (true, null, MapSupplierOrderToDto(created));
    }

    public async Task<(bool Success, string? Error)> UpdateSupplierOrderStatusAsync(int orderId, SlnSupplierOrderStatusUpdateDto dto, int personnelId, int customerId, int? branchId = null)
    {
        if (SalonSupplierOrderStatuses.GetById(dto.StatusId) == null)
            return (false, "Gecersiz siparis durumu");

        var order = await _supplierOrders.GetAllQueryable()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        if (order == null) return (false, "Siparis bulunamadi");

        if (order.StatusId == SalonSupplierOrderStatuses.Ids.Received && dto.StatusId == SalonSupplierOrderStatuses.Ids.Received)
            return (true, null);

        if (order.StatusId == SalonSupplierOrderStatuses.Ids.Received && dto.StatusId != SalonSupplierOrderStatuses.Ids.Received)
            return (false, "Teslim alinmis siparis geri alinamaz");

        if (dto.StatusId == SalonSupplierOrderStatuses.Ids.Received)
        {
            var effectiveBranchId = await _stockBalances.ResolveBranchIdAsync(customerId, branchId);
            if (!effectiveBranchId.HasValue) return (false, "Stok islenecek sube bulunamadi");

            foreach (var item in order.Items)
            {
                var product = item.Product;
                if (product == null)
                    return (false, "Siparis urunu bulunamadi");

                var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                    product, customerId, effectiveBranchId.Value, item.Quantity, preventNegative: false);
                if (!stockOk) return (false, stockError);

                await _stockBalances.SyncProductTotalAsync(product, customerId);
                item.ReceivedQuantity = item.Quantity;

                _stockMovements.Add(new SlnStockMovement
                {
                    CustomerId = customerId,
                    BranchId = effectiveBranchId.Value,
                    ProductId = item.ProductId,
                    MovementTypeId = 1,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SupplierId = order.SupplierId,
                    Notes = $"Tedarik siparisi teslim: {order.OrderNo}",
                    CreatedByPersonnelId = personnelId > 0 ? personnelId : null
                });
            }

            var amount = Math.Round(order.Items.Sum(i => i.Quantity * i.UnitPrice), 2, MidpointRounding.AwayFromZero);
            if (amount > 0)
            {
                _supplierTransactions.Add(new SlnSupplierTransaction
                {
                    SupplierId = order.SupplierId,
                    TransactionTypeId = 1,
                    Amount = amount,
                    Description = $"Tedarik siparisi teslim: {order.OrderNo}",
                    TransactionDate = DateTime.UtcNow
                });
            }

            order.ReceivedAt = DateTime.UtcNow;
        }

        order.StatusId = dto.StatusId;
        order.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? dto.Notes.Trim() : $"{order.Notes}\n{dto.Notes.Trim()}";

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Tedarik siparisi durumu guncellendi. CustomerId={CustomerId} OrderId={OrderId} StatusId={StatusId}",
            customerId, orderId, dto.StatusId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddStockMovementAsync(int productId, int movementTypeId, decimal quantity, decimal unitPrice, int? supplierId, string? notes, int personnelId, int customerId, int? branchId = null)
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

        var resolvedBranchId = await _stockBalances.ResolveBranchIdAsync(customerId, branchId);
        if (!resolvedBranchId.HasValue) return (false, "Sube bulunamadi");

        var movement = new SlnStockMovement
        {
            CustomerId = customerId,
            BranchId = resolvedBranchId.Value,
            ProductId = productId,
            MovementTypeId = movementTypeId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            SupplierId = supplierId,
            Notes = notes,
            CreatedByPersonnelId = personnelId > 0 ? personnelId : null
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

        var delta = movementTypeId switch
        {
            1 => quantity,   // Alis
            2 => -quantity,  // Satis
            3 => -quantity,  // Dahili kullanim
            5 => quantity,   // Iade
            _ => 0
        };
        var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
            product, customerId, resolvedBranchId.Value, delta, preventNegative: delta < 0);
        if (!stockOk) return (false, stockError);
        await _stockBalances.SyncProductTotalAsync(product, customerId);

        _stockMovements.Add(movement);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Stok hareketi eklendi. CustomerId={CustomerId} PersonnelId={PersonnelId} BranchId={BranchId} ProductId={ProductId} MovementTypeId={MovementTypeId} Quantity={Quantity} UnitPrice={UnitPrice}",
            customerId, personnelId, movement.BranchId, productId, movementTypeId, quantity, unitPrice);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> TransferStockAsync(
        int productId,
        int? fromBranchId,
        int toBranchId,
        decimal quantity,
        string? notes,
        int personnelId,
        int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);
        if (product == null) return (false, "Urun bulunamadi");
        if (quantity <= 0) return (false, "Transfer miktari 0'dan buyuk olmali");

        var effectiveFromBranchId = await _stockBalances.ResolveBranchIdAsync(customerId, fromBranchId);
        if (!effectiveFromBranchId.HasValue) return (false, "Kaynak sube bulunamadi");

        if (effectiveFromBranchId.Value == toBranchId)
            return (false, "Kaynak ve hedef sube ayni olamaz");

        if (!await BranchExistsAsync(effectiveFromBranchId.Value, customerId))
            return (false, "Kaynak sube bulunamadi");
        if (!await BranchExistsAsync(toBranchId, customerId))
            return (false, "Hedef sube bulunamadi");

        var currentFromStock = await _stockBalances.GetStockQuantityAsync(customerId, productId, effectiveFromBranchId.Value, product.StockQuantity);
        if (currentFromStock < quantity) return (false, "Transfer icin yeterli sube stogu yok");

        var unitPrice = product.PurchasePrice;
        var transferUid = Guid.NewGuid().ToString("N");
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : " - " + notes.Trim();

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = effectiveFromBranchId.Value,
            MovementTypeId = 4,
            Quantity = -quantity,
            UnitPrice = unitPrice,
            Notes = $"TransferOut:{transferUid}|ToBranch:{toBranchId}{cleanNotes}",
            CreatedByPersonnelId = personnelId > 0 ? personnelId : null
        });

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = toBranchId,
            MovementTypeId = 4,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Notes = $"TransferIn:{transferUid}|FromBranch:{effectiveFromBranchId.Value}{cleanNotes}",
            CreatedByPersonnelId = personnelId > 0 ? personnelId : null
        });

        var (fromOk, fromError) = await _stockBalances.AdjustStockAsync(product, customerId, effectiveFromBranchId.Value, -quantity, preventNegative: true);
        if (!fromOk) return (false, fromError);
        var (toOk, toError) = await _stockBalances.AdjustStockAsync(product, customerId, toBranchId, quantity, preventNegative: false);
        if (!toOk) return (false, toError);
        await _stockBalances.SyncProductTotalAsync(product, customerId);

        await _uow.SaveChangesAsync();
        _logger.LogInformation(
            "Stok transferi audit kaydi olustu. CustomerId={CustomerId} PersonnelId={PersonnelId} ProductId={ProductId} FromBranchId={FromBranchId} ToBranchId={ToBranchId} Quantity={Quantity}",
            customerId, personnelId, productId, effectiveFromBranchId.Value, toBranchId, quantity);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AdjustStockCountAsync(
        int productId,
        int? branchId,
        decimal countedQuantity,
        string? notes,
        int personnelId,
        int customerId)
    {
        var product = await _products.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == customerId);
        if (product == null) return (false, "Urun bulunamadi");
        if (countedQuantity < 0) return (false, "Sayilan stok negatif olamaz");
        var effectiveBranchId = await _stockBalances.ResolveBranchIdAsync(customerId, branchId);
        if (!effectiveBranchId.HasValue)
            return (false, "Sube bulunamadi");

        var before = await _stockBalances.GetStockQuantityAsync(customerId, product.Id, effectiveBranchId.Value, product.StockQuantity);
        var difference = countedQuantity - before;
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : " - " + notes.Trim();

        _stockMovements.Add(new SlnStockMovement
        {
            CustomerId = customerId,
            ProductId = productId,
            BranchId = effectiveBranchId.Value,
            MovementTypeId = 6,
            Quantity = difference,
            UnitPrice = product.PurchasePrice,
            Notes = $"StockCount|Before:{before:0.##}|Counted:{countedQuantity:0.##}|Diff:{difference:0.##}{cleanNotes}",
            CreatedByPersonnelId = personnelId > 0 ? personnelId : null
        });

        await _stockBalances.SetStockQuantityAsync(customerId, product.Id, effectiveBranchId.Value, countedQuantity);
        await _stockBalances.SyncProductTotalAsync(product, customerId);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Stok sayim farki kaydedildi. CustomerId={CustomerId} PersonnelId={PersonnelId} BranchId={BranchId} ProductId={ProductId} Before={Before} Counted={Counted} Difference={Difference}",
            customerId, personnelId, effectiveBranchId.Value, productId, before, countedQuantity, difference);
        return (true, null);
    }

    private async Task<bool> BranchExistsAsync(int branchId, int customerId)
        => await _branches.GetAllQueryable().AnyAsync(b => b.Id == branchId && b.CustomerId == customerId && b.IsActive);

    private static string BuildPurchaseDescription(SlnProduct product, decimal quantity, decimal unitPrice, string? notes)
    {
        var description = $"Alis kaydi: {product.Name} ({quantity:0.##} {product.Unit} x {unitPrice:0.##} TL)";
        return string.IsNullOrWhiteSpace(notes) ? description : $"{description} - {notes.Trim()}";
    }

    private static SlnProductDto MapProductToDto(SlnProduct p, decimal stockQuantity) => new()
    {
        Id = p.Id,
        BranchId = p.BranchId,
        CategoryId = p.CategoryId,
        BrandId = p.BrandId,
        Name = p.Name,
        Barcode = p.Barcode,
        CategoryName = p.Category?.Name ?? "",
        BrandName = p.Brand?.Name,
        PurchasePrice = p.PurchasePrice,
        SalePrice = p.SalePrice,
        StockQuantity = stockQuantity,
        MinStockLevel = p.MinStockLevel,
        Unit = p.Unit,
        IsActive = p.IsActive,
        IsLowStock = p.MinStockLevel > 0 && stockQuantity <= p.MinStockLevel,
        SuggestedOrderQuantity = CalculateSuggestedOrderQuantity(p, stockQuantity)
    };

    private static decimal ResolveStockFallback(int? branchId, decimal productTotalStock)
        => branchId.HasValue ? 0m : productTotalStock;

    private static string NormalizeBarcode(string? barcode)
        => (barcode ?? string.Empty).Trim().ToLowerInvariant();

    private async Task<string> BuildOrderNoAsync(int customerId)
    {
        var prefix = $"SO-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _supplierOrders.GetAllQueryable()
            .CountAsync(o => o.CustomerId == customerId && o.OrderNo.StartsWith(prefix));
        return $"{prefix}{count + 1:000}";
    }

    private static decimal CalculateSuggestedOrderQuantity(SlnProduct p, decimal stockQuantity)
    {
        if (p.MinStockLevel <= 0) return 0;
        var target = p.MinStockLevel * 2;
        var needed = target - stockQuantity;
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
