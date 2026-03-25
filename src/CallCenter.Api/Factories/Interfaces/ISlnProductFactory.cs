using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnProductFactory
{
    // Urun
    Task<List<SlnProductDto>> GetProductsAsync(int customerId, int? categoryId = null, string? search = null);
    Task<SlnProductDto?> GetProductAsync(int productId, int customerId);
    Task<SlnProductDto> CreateProductAsync(SlnProductCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateProductAsync(int productId, SlnProductCreateDto dto, bool isActive, int customerId);
    Task<(bool Success, string? Error)> DeleteProductAsync(int productId, int customerId);

    // Kategori
    Task<List<object>> GetCategoriesAsync(int customerId);
    Task<object> CreateCategoryAsync(string name, int sortOrder, int customerId);
    Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, int customerId);
    Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId);

    // Marka
    Task<List<object>> GetBrandsAsync(int customerId);
    Task<object> CreateBrandAsync(string name, int customerId);
    Task<(bool Success, string? Error)> UpdateBrandAsync(int brandId, string name, int customerId);
    Task<(bool Success, string? Error)> DeleteBrandAsync(int brandId, int customerId);

    // Tedarikci
    Task<List<SlnSupplierDto>> GetSuppliersAsync(int customerId);
    Task<SlnSupplierDto> CreateSupplierAsync(SlnSupplierCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateSupplierAsync(int supplierId, SlnSupplierCreateDto dto, bool isActive, int customerId);
    Task<(bool Success, string? Error)> DeleteSupplierAsync(int supplierId, int customerId);

    // Stok hareket
    Task<(bool Success, string? Error)> AddStockMovementAsync(int productId, int movementTypeId, decimal quantity, decimal unitPrice, int? supplierId, string? notes, int userId, int customerId);
}
