using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnDataImportFactory : ISlnDataImportFactory
{
    private const int MaxRows = 5000;

    private readonly ISlnClientEntityService _clients;
    private readonly ISlnServiceCategoryEntityService _serviceCategories;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnProductCategoryEntityService _productCategories;
    private readonly ISlnProductBrandEntityService _productBrands;
    private readonly ISlnProductEntityService _products;
    private readonly ISlnStockMovementEntityService _stockMovements;
    private readonly ISlnBranchEntityService _branches;
    private readonly IUserEntityService _users;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnPersonnelSkillEntityService _skills;
    private readonly ISlnStockBalanceService _stockBalances;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnDataImportFactory> _logger;

    public SlnDataImportFactory(
        ISlnClientEntityService clients,
        ISlnServiceCategoryEntityService serviceCategories,
        ISlnServiceEntityService services,
        ISlnProductCategoryEntityService productCategories,
        ISlnProductBrandEntityService productBrands,
        ISlnProductEntityService products,
        ISlnStockMovementEntityService stockMovements,
        ISlnBranchEntityService branches,
        IUserEntityService users,
        ICustomerPersonnelEntityService personnel,
        ISlnPersonnelSkillEntityService skills,
        ISlnStockBalanceService stockBalances,
        IPasswordPolicyFactory passwordPolicy,
        IUnitOfWork uow,
        ILogger<SlnDataImportFactory> logger)
    {
        _clients = clients;
        _serviceCategories = serviceCategories;
        _services = services;
        _productCategories = productCategories;
        _productBrands = productBrands;
        _products = products;
        _stockMovements = stockMovements;
        _branches = branches;
        _users = users;
        _personnel = personnel;
        _skills = skills;
        _stockBalances = stockBalances;
        _passwordPolicy = passwordPolicy;
        _uow = uow;
        _logger = logger;
    }

    public List<SlnDataImportTemplateDto> GetTemplates() => BuildTemplates();

    public byte[] BuildExcelTemplate(string importType, out string fileName)
    {
        var template = GetTemplate(importType);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");
        for (var i = 0; i < template.Columns.Count; i++)
        {
            var column = template.Columns[i];
            var cell = ws.Cell(1, i + 1);
            cell.Value = column.Label;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = column.Required ? XLColor.FromHtml("#DDEBFF") : XLColor.FromHtml("#F8F9FA");
            if (!string.IsNullOrWhiteSpace(column.Description))
                cell.GetComment().AddText(column.Description);
        }

        var sample = BuildSampleRow(template.ImportType);
        for (var i = 0; i < sample.Length; i++)
            ws.Cell(2, i + 1).Value = sample[i];

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        fileName = $"corplynk-salon-{template.ImportType}-import-template.xlsx";
        return stream.ToArray();
    }

    public async Task<SlnDataImportPreviewDto> PreviewAsync(string importType, Stream stream, string fileName, int customerId, int? branchId)
        => await BuildResultAsync(importType, stream, fileName, customerId, userId: null, branchId, commit: false);

    public async Task<SlnDataImportPreviewDto> ImportAsync(string importType, Stream stream, string fileName, int customerId, int userId, int? branchId)
        => await BuildResultAsync(importType, stream, fileName, customerId, userId, branchId, commit: true);

    private async Task<SlnDataImportPreviewDto> BuildResultAsync(
        string importType,
        Stream stream,
        string fileName,
        int customerId,
        int? userId,
        int? branchId,
        bool commit)
    {
        var template = GetTemplate(importType);
        var table = ReadTable(stream, fileName);
        var branches = await LoadBranchesAsync(customerId);
        var result = new SlnDataImportPreviewDto
        {
            ImportType = template.ImportType,
            FileName = fileName,
            DetectedColumns = table.Headers,
            ExpectedColumns = template.Columns
        };

        if (table.Rows.Count > MaxRows)
        {
            result.Rows.Add(ErrorRow(1, $"Tek seferde en fazla {MaxRows} satir aktarilabilir."));
            result.TotalRows = table.Rows.Count;
            result.ErrorRows = 1;
            return result;
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceRow in table.Rows)
        {
            var row = template.ImportType switch
            {
                SlnDataImportTypes.Clients => await BuildClientRowAsync(sourceRow, customerId, branches, branchId, seenKeys, commit),
                SlnDataImportTypes.Services => await BuildServiceRowAsync(sourceRow, customerId, seenKeys, commit),
                SlnDataImportTypes.Personnel => await BuildPersonnelRowAsync(sourceRow, customerId, branches, branchId, seenKeys, commit),
                SlnDataImportTypes.Products => await BuildProductRowAsync(sourceRow, customerId, branches, branchId, userId, seenKeys, commit),
                _ => ErrorRow(sourceRow.RowNumber, "Gecersiz veri tipi.")
            };
            result.Rows.Add(row);
        }

        result.TotalRows = result.Rows.Count;
        result.ReadyRows = result.Rows.Count(r => r.Status == "ready");
        result.WarningRows = result.Rows.Count(r => r.Status == "warning");
        result.DuplicateRows = result.Rows.Count(r => r.Status == "duplicate");
        result.ErrorRows = result.Rows.Count(r => r.Status == "error");
        result.ImportedRows = result.Rows.Count(r => r.Status == "imported");
        result.SkippedRows = result.Rows.Count(r => r.Status is "skipped" or "duplicate" or "error");

        if (commit)
        {
            await _uow.SaveChangesAsync();
            _logger.LogInformation(
                "Salon data import tamamlandi. CustomerId={CustomerId} UserId={UserId} Type={ImportType} Imported={ImportedRows} Skipped={SkippedRows} FileName={FileName}",
                customerId, userId, result.ImportType, result.ImportedRows, result.SkippedRows, fileName);
        }

        return result;
    }

    private async Task<SlnDataImportRowDto> BuildClientRowAsync(
        SourceRow row,
        int customerId,
        List<SlnBranch> branches,
        int? requestedBranchId,
        HashSet<string> seenKeys,
        bool commit)
    {
        var fullName = Clean(Get(row, "fullName"));
        var phone = Clean(Get(row, "phone"));
        var email = Clean(Get(row, "email"));
        var normalizedPhone = NormalizePhone(phone);
        var duplicateKey = !string.IsNullOrWhiteSpace(normalizedPhone) ? $"phone:{normalizedPhone}" : $"email:{NormalizeEmail(email)}";

        var dto = BaseRow(row);
        dto.Normalized["fullName"] = fullName;
        dto.Normalized["phone"] = phone;
        dto.Normalized["email"] = email;
        dto.Normalized["branchName"] = Clean(Get(row, "branch"));

        if (string.IsNullOrWhiteSpace(fullName))
            return Mark(dto, "error", "Ad soyad zorunlu.");
        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
            return Mark(dto, "error", "Telefon veya e-posta alanlarindan biri zorunlu.");
        if (!string.IsNullOrWhiteSpace(email) && !IsLikelyEmail(email))
            return Mark(dto, "error", "E-posta formati gecersiz.");
        if (!string.IsNullOrWhiteSpace(duplicateKey) && !seenKeys.Add(duplicateKey))
            return Mark(dto, "duplicate", "Dosya icinde ayni telefon/e-posta tekrar ediyor.");

        var exists = await _clients.GetAllQueryable().AnyAsync(c => c.CustomerId == customerId
            && ((normalizedPhone != "" && c.Phone != null && NormalizePhone(c.Phone) == normalizedPhone)
                || (!string.IsNullOrWhiteSpace(email) && c.Email != null && c.Email.ToLower() == email.ToLower())));
        if (exists)
            return Mark(dto, "duplicate", "Bu musteri zaten kayitli gorunuyor.");

        var branchId = ResolveBranchId(row, branches, requestedBranchId);
        if (branchId.Error != null)
            return Mark(dto, "error", branchId.Error);

        if (!commit)
            return Mark(dto, "ready", "Aktarima hazir.");

        _clients.Add(new SlnClient
        {
            CustomerId = customerId,
            BranchId = branchId.Value,
            FullName = fullName,
            Phone = phone,
            Phone2 = Clean(Get(row, "phone2")),
            Email = email,
            BirthDate = ParseDate(Get(row, "birthDate")),
            City = Clean(Get(row, "city")),
            Address = Clean(Get(row, "address")),
            Notes = Clean(Get(row, "notes")),
            IsActive = true
        });

        return Mark(dto, "imported", "Musteri aktarildi.");
    }

    private async Task<SlnDataImportRowDto> BuildServiceRowAsync(
        SourceRow row,
        int customerId,
        HashSet<string> seenKeys,
        bool commit)
    {
        var name = Clean(Get(row, "name"));
        var categoryName = Clean(Get(row, "category"));
        var duration = ParseInt(Get(row, "durationMinutes"), 30);
        var price = ParseDecimal(Get(row, "price"), 0);

        var dto = BaseRow(row);
        dto.Normalized["name"] = name;
        dto.Normalized["category"] = categoryName;
        dto.Normalized["durationMinutes"] = duration.ToString(CultureInfo.InvariantCulture);
        dto.Normalized["price"] = price.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(name))
            return Mark(dto, "error", "Hizmet adi zorunlu.");
        if (duration <= 0)
            return Mark(dto, "error", "Sure 0'dan buyuk olmali.");
        if (price < 0)
            return Mark(dto, "error", "Fiyat negatif olamaz.");

        categoryName = string.IsNullOrWhiteSpace(categoryName) ? "Genel" : categoryName;
        var key = $"{NormalizeText(categoryName)}|{NormalizeText(name)}";
        if (!seenKeys.Add(key))
            return Mark(dto, "duplicate", "Dosya icinde ayni kategori/hizmet tekrar ediyor.");

        var exists = await _services.GetAllQueryable()
            .Include(s => s.Category)
            .AnyAsync(s => s.CustomerId == customerId
                && s.Name.ToLower() == name.ToLower()
                && s.Category != null
                && s.Category.Name.ToLower() == categoryName.ToLower());
        if (exists)
            return Mark(dto, "duplicate", "Bu hizmet zaten kayitli gorunuyor.");

        if (!commit)
            return Mark(dto, "ready", "Aktarima hazir.");

        var category = await GetOrCreateServiceCategoryAsync(customerId, categoryName);
        _services.Add(new SlnService
        {
            CustomerId = customerId,
            CategoryId = category.Id,
            Name = name,
            DurationMinutes = duration,
            Price = price,
            TaxRate = ParseDecimal(Get(row, "taxRate"), 10),
            SortOrder = ParseInt(Get(row, "sortOrder"), 0),
            IsActive = true
        });

        return Mark(dto, "imported", "Hizmet aktarildi.");
    }

    private async Task<SlnDataImportRowDto> BuildPersonnelRowAsync(
        SourceRow row,
        int customerId,
        List<SlnBranch> branches,
        int? requestedBranchId,
        HashSet<string> seenKeys,
        bool commit)
    {
        var fullName = Clean(Get(row, "fullName"));
        var title = Clean(Get(row, "title"));
        var roleId = ResolveSalonRoleId(Get(row, "role"));
        var email = Clean(Get(row, "email"));
        var requestedUserName = Clean(Get(row, "userName"));
        var userName = string.IsNullOrWhiteSpace(requestedUserName) ? BuildUserName(fullName ?? string.Empty, customerId) : ToSlug(requestedUserName);

        var dto = BaseRow(row);
        dto.Normalized["fullName"] = fullName;
        dto.Normalized["userName"] = userName;
        dto.Normalized["email"] = email;
        dto.Normalized["role"] = SalonRoles.GetById(roleId)?.Description;
        dto.Normalized["branchName"] = Clean(Get(row, "branch"));

        if (string.IsNullOrWhiteSpace(fullName))
            return Mark(dto, "error", "Personel adi zorunlu.");
        if (string.IsNullOrWhiteSpace(userName))
            return Mark(dto, "error", "Kullanici adi uretilemedi.");
        if (!string.IsNullOrWhiteSpace(email) && !IsLikelyEmail(email))
            return Mark(dto, "error", "E-posta formati gecersiz.");
        if (!seenKeys.Add($"username:{userName}"))
            return Mark(dto, "duplicate", "Dosya icinde ayni kullanici adi tekrar ediyor.");
        if (await _users.ExistsByUsernameAsync(userName))
            return Mark(dto, "duplicate", "Bu kullanici adi zaten kayitli.");

        if (string.IsNullOrWhiteSpace(email))
            email = $"import-{customerId}-{userName}@corplynk.local";
        if (await _users.ExistsByEmailAsync(email))
            return Mark(dto, "duplicate", "Bu e-posta zaten kayitli.");

        var branchId = ResolveBranchId(row, branches, requestedBranchId);
        if (branchId.Error != null)
            return Mark(dto, "error", branchId.Error);

        var skillNames = SplitList(Get(row, "services"));
        var skillIds = await ResolveServiceIdsAsync(customerId, skillNames);
        if (skillIds.Error != null)
            return Mark(dto, "error", skillIds.Error);

        if (!commit)
            return Mark(dto, "ready", "Aktarima hazir.");

        var tempPassword = _passwordPolicy.GenerateSecureTemporaryPassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        var user = new User
        {
            UserName = userName,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            RoleId = UserRoles.Ids.CustomerUser,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow,
            MustChangePassword = true
        };
        _users.Add(user);
        await _uow.SaveChangesAsync();
        await _passwordPolicy.RecordPasswordAsync(user.Id, passwordHash);

        var personnel = new CustomerPersonnel
        {
            UserId = user.Id,
            CustomerId = customerId,
            Title = title ?? string.Empty,
            CustomerRoleId = roleId,
            BranchId = branchId.Value,
            IsActive = true,
            PublicVisible = true,
            PublicShowFullName = true,
            PublicShowPhoto = true,
            PublicShowTitle = true,
            PublicShowSpecialty = true
        };
        _personnel.Add(personnel);
        await _uow.SaveChangesAsync();

        foreach (var serviceId in skillIds.Value)
            _skills.Add(new SlnPersonnelSkill { PersonnelId = personnel.Id, ServiceId = serviceId });

        dto.Normalized["temporaryPassword"] = tempPassword;
        return Mark(dto, "imported", "Personel aktarildi. Gecici sifre sonuc satirinda gosterildi.");
    }

    private async Task<SlnDataImportRowDto> BuildProductRowAsync(
        SourceRow row,
        int customerId,
        List<SlnBranch> branches,
        int? requestedBranchId,
        int? userId,
        HashSet<string> seenKeys,
        bool commit)
    {
        var name = Clean(Get(row, "name"));
        var categoryName = Clean(Get(row, "category"));
        var brandName = Clean(Get(row, "brand"));
        var barcode = Clean(Get(row, "barcode"));
        var purchasePrice = ParseDecimal(Get(row, "purchasePrice"), 0);
        var salePrice = ParseDecimal(Get(row, "salePrice"), 0);
        var stockQuantity = ParseDecimal(Get(row, "stockQuantity"), 0);

        var dto = BaseRow(row);
        dto.Normalized["name"] = name;
        dto.Normalized["category"] = categoryName;
        dto.Normalized["brand"] = brandName;
        dto.Normalized["barcode"] = barcode;
        dto.Normalized["stockQuantity"] = stockQuantity.ToString(CultureInfo.InvariantCulture);
        dto.Normalized["branchName"] = Clean(Get(row, "branch"));

        if (string.IsNullOrWhiteSpace(name))
            return Mark(dto, "error", "Urun adi zorunlu.");
        if (purchasePrice < 0 || salePrice < 0 || stockQuantity < 0)
            return Mark(dto, "error", "Fiyat ve stok degerleri negatif olamaz.");
        if (salePrice == 0 && purchasePrice == 0)
            return Mark(dto, "error", "Alis veya satis fiyatindan en az biri girilmeli.");

        categoryName = string.IsNullOrWhiteSpace(categoryName) ? "Genel" : categoryName;
        var branchId = ResolveBranchId(row, branches, requestedBranchId);
        if (branchId.Error != null)
            return Mark(dto, "error", branchId.Error);
        if (!branchId.Value.HasValue)
            return Mark(dto, "error", "Urun/stok importu icin sube secilmeli veya dosyada Sube kolonu bulunmali.");

        var key = !string.IsNullOrWhiteSpace(barcode)
            ? $"barcode:{NormalizeText(barcode)}"
            : $"product:{NormalizeText(categoryName)}|{NormalizeText(brandName)}|{NormalizeText(name)}|{branchId.Value}";
        if (!seenKeys.Add(key))
            return Mark(dto, "duplicate", "Dosya icinde ayni urun tekrar ediyor.");

        var exists = await _products.GetAllQueryable().AnyAsync(p => p.CustomerId == customerId
            && ((!string.IsNullOrWhiteSpace(barcode) && p.Barcode != null && p.Barcode.ToLower() == barcode.ToLower())
                || (p.Name.ToLower() == name.ToLower() && p.BranchId == branchId.Value)));
        if (exists)
            return Mark(dto, "duplicate", "Bu urun zaten kayitli gorunuyor.");

        if (!commit)
            return Mark(dto, "ready", "Aktarima hazir.");

        var category = await GetOrCreateProductCategoryAsync(customerId, categoryName);
        var brand = await GetOrCreateProductBrandAsync(customerId, brandName);
        var product = new SlnProduct
        {
            CustomerId = customerId,
            BranchId = branchId.Value,
            CategoryId = category.Id,
            BrandId = brand?.Id,
            Name = name,
            Barcode = barcode,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice > 0 ? salePrice : purchasePrice,
            StockQuantity = 0,
            MinStockLevel = ParseDecimal(Get(row, "minStockLevel"), 0),
            Unit = Clean(Get(row, "unit")) ?? "Adet",
            IsActive = true
        };
        _products.Add(product);
        await _uow.SaveChangesAsync();

        await _stockBalances.SetStockQuantityAsync(customerId, product.Id, branchId.Value.Value, stockQuantity);
        await _stockBalances.SyncProductTotalAsync(product, customerId);
        if (stockQuantity > 0)
        {
            _stockMovements.Add(new SlnStockMovement
            {
                CustomerId = customerId,
                ProductId = product.Id,
                BranchId = branchId.Value.Value,
                MovementTypeId = 6,
                Quantity = stockQuantity,
                UnitPrice = product.PurchasePrice,
                Notes = "DataImport|OpeningStock",
                CreatedByPersonnelId = userId
            });
        }

        return Mark(dto, "imported", "Urun ve stok aktarildi.");
    }

    private async Task<SlnServiceCategory> GetOrCreateServiceCategoryAsync(int customerId, string name)
    {
        var normalized = name.Trim();
        var existing = await _serviceCategories.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Name.ToLower() == normalized.ToLower());
        if (existing != null) return existing;

        var sortOrder = await _serviceCategories.GetAllQueryable().CountAsync(c => c.CustomerId == customerId) + 1;
        var category = new SlnServiceCategory { CustomerId = customerId, Name = normalized, SortOrder = sortOrder, IsActive = true };
        _serviceCategories.Add(category);
        await _uow.SaveChangesAsync();
        return category;
    }

    private async Task<SlnProductCategory> GetOrCreateProductCategoryAsync(int customerId, string name)
    {
        var normalized = name.Trim();
        var existing = await _productCategories.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Name.ToLower() == normalized.ToLower());
        if (existing != null) return existing;

        var sortOrder = await _productCategories.GetAllQueryable().CountAsync(c => c.CustomerId == customerId) + 1;
        var category = new SlnProductCategory { CustomerId = customerId, Name = normalized, SortOrder = sortOrder };
        _productCategories.Add(category);
        await _uow.SaveChangesAsync();
        return category;
    }

    private async Task<SlnProductBrand?> GetOrCreateProductBrandAsync(int customerId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var normalized = name.Trim();
        var existing = await _productBrands.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.Name.ToLower() == normalized.ToLower());
        if (existing != null) return existing;

        var brand = new SlnProductBrand { CustomerId = customerId, Name = normalized };
        _productBrands.Add(brand);
        await _uow.SaveChangesAsync();
        return brand;
    }

    private async Task<(List<int> Value, string? Error)> ResolveServiceIdsAsync(int customerId, List<string> serviceNames)
    {
        if (serviceNames.Count == 0) return ([], null);

        var services = await _services.GetAllQueryable()
            .Where(s => s.CustomerId == customerId)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();
        var result = new List<int>();
        foreach (var name in serviceNames)
        {
            var match = services.FirstOrDefault(s => NormalizeText(s.Name) == NormalizeText(name));
            if (match == null) return ([], $"Personel hizmeti bulunamadi: {name}");
            result.Add(match.Id);
        }

        return (result.Distinct().ToList(), null);
    }

    private async Task<List<SlnBranch>> LoadBranchesAsync(int customerId)
        => await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.IsActive)
            .OrderByDescending(b => b.IsHeadquarter)
            .ThenBy(b => b.Name)
            .ToListAsync();

    private static (int? Value, string? Error) ResolveBranchId(SourceRow row, List<SlnBranch> branches, int? requestedBranchId)
    {
        var branchName = Clean(Get(row, "branch"));
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            var match = branches.FirstOrDefault(b => NormalizeText(b.Name) == NormalizeText(branchName));
            return match != null ? (match.Id, null) : (null, $"Sube bulunamadi: {branchName}");
        }

        if (requestedBranchId.HasValue)
        {
            return branches.Any(b => b.Id == requestedBranchId.Value)
                ? (requestedBranchId.Value, null)
                : (null, "Secilen sube bulunamadi.");
        }

        var defaultBranch = branches.FirstOrDefault(b => b.IsHeadquarter) ?? branches.FirstOrDefault();
        return (defaultBranch?.Id, null);
    }

    private static List<SlnDataImportTemplateDto> BuildTemplates() =>
    [
        new()
        {
            ImportType = SlnDataImportTypes.Clients,
            DisplayName = "Musteriler",
            Columns =
            [
                Col("fullName", "Ad Soyad", true, "Musteri adi soyadi"),
                Col("phone", "Telefon", false, "Tekil eslestirme icin onerilir"),
                Col("phone2", "Telefon 2", false, null),
                Col("email", "E-posta", false, null),
                Col("birthDate", "Dogum Tarihi", false, "gg.aa.yyyy"),
                Col("city", "Sehir", false, null),
                Col("address", "Adres", false, null),
                Col("notes", "Not", false, null),
                Col("branch", "Sube", false, "Bos kalirsa secili/ana sube kullanilir")
            ]
        },
        new()
        {
            ImportType = SlnDataImportTypes.Services,
            DisplayName = "Hizmetler",
            Columns =
            [
                Col("name", "Hizmet Adi", true, null),
                Col("category", "Kategori", false, "Bos kalirsa Genel"),
                Col("durationMinutes", "Sure Dakika", false, "Bos kalirsa 30"),
                Col("price", "Fiyat", false, null),
                Col("taxRate", "KDV", false, "Bos kalirsa 10"),
                Col("sortOrder", "Sira", false, null)
            ]
        },
        new()
        {
            ImportType = SlnDataImportTypes.Personnel,
            DisplayName = "Personel",
            Columns =
            [
                Col("fullName", "Ad Soyad", true, null),
                Col("title", "Unvan", false, null),
                Col("role", "Rol", false, "Kuafor, Guzellik Uzmani, Resepsiyonist, Kasiyer, Mudur, Sube Muduru"),
                Col("email", "E-posta", false, "Bos kalirsa import adresi uretilir"),
                Col("userName", "Kullanici Adi", false, "Bos kalirsa ad soyaddan uretilir"),
                Col("branch", "Sube", false, "Bos kalirsa secili/ana sube kullanilir"),
                Col("services", "Hizmetler", false, "Virgulle ayirilmis hizmet adlari")
            ]
        },
        new()
        {
            ImportType = SlnDataImportTypes.Products,
            DisplayName = "Urunler ve Stok",
            Columns =
            [
                Col("name", "Urun Adi", true, null),
                Col("category", "Kategori", false, "Bos kalirsa Genel"),
                Col("brand", "Marka", false, null),
                Col("barcode", "Barkod", false, null),
                Col("purchasePrice", "Alis Fiyati", false, null),
                Col("salePrice", "Satis Fiyati", false, null),
                Col("stockQuantity", "Stok", false, "Bos kalirsa 0"),
                Col("minStockLevel", "Kritik Stok", false, null),
                Col("unit", "Birim", false, "Bos kalirsa Adet"),
                Col("branch", "Sube", false, "Bos kalirsa secili/ana sube kullanilir")
            ]
        }
    ];

    private static SlnDataImportTemplateDto GetTemplate(string importType)
    {
        var key = NormalizeImportType(importType);
        return BuildTemplates().FirstOrDefault(t => t.ImportType == key)
            ?? throw new InvalidOperationException("Gecersiz import tipi.");
    }

    private static SlnDataImportTemplateColumnDto Col(string key, string label, bool required, string? description) =>
        new() { Key = key, Label = label, Required = required, Description = description };

    private static string[] BuildSampleRow(string importType) => importType switch
    {
        SlnDataImportTypes.Clients => ["Deniz Yilmaz", "05060000000", "", "deniz@example.com", "01.01.1990", "Istanbul", "Adres", "Not", "Merkez"],
        SlnDataImportTypes.Services => ["Sac Kesim", "Sac", "45", "750", "10", "1"],
        SlnDataImportTypes.Personnel => ["Ayse Uzman", "Uzman", "Guzellik Uzmani", "ayse@example.com", "ayseuzman", "Merkez", "Sac Kesim, Cilt Bakimi"],
        SlnDataImportTypes.Products => ["Bakim Serumu", "Bakim", "Demo Marka", "8690000000000", "250", "450", "10", "2", "Adet", "Merkez"],
        _ => []
    };

    private static ParsedTable ReadTable(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".xlsx" or ".xlsm" or ".xltx"
            ? ReadExcel(stream)
            : throw new InvalidOperationException("Sadece Excel XLSX dosyalari desteklenir.");
    }

    private static ParsedTable ReadExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var used = ws.RangeUsed();
        if (used == null) return new ParsedTable([], []);

        var headers = used.FirstRow().Cells().Select(c => c.GetString().Trim()).ToList();
        var rows = new List<SourceRow>();
        foreach (var row in used.RowsUsed().Skip(1))
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (string.IsNullOrWhiteSpace(header)) continue;
                values[header] = row.Cell(i + 1).GetFormattedString().Trim();
            }
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;
            rows.Add(new SourceRow(row.RowNumber(), values));
        }
        return new ParsedTable(headers.Where(h => !string.IsNullOrWhiteSpace(h)).ToList(), rows);
    }

    private static SlnDataImportRowDto BaseRow(SourceRow row) =>
        new() { RowNumber = row.RowNumber, Raw = new(row.Values, StringComparer.OrdinalIgnoreCase) };

    private static SlnDataImportRowDto ErrorRow(int rowNumber, string message) =>
        new() { RowNumber = rowNumber, Status = "error", Message = message };

    private static SlnDataImportRowDto Mark(SlnDataImportRowDto row, string status, string message)
    {
        row.Status = status;
        row.Message = message;
        return row;
    }

    private static string NormalizeImportType(string? importType)
    {
        var value = (importType ?? "").Trim().ToLowerInvariant();
        return value switch
        {
            "client" or "clients" or "musteri" or "musteriler" => SlnDataImportTypes.Clients,
            "service" or "services" or "hizmet" or "hizmetler" => SlnDataImportTypes.Services,
            "staff" or "personnel" or "personel" => SlnDataImportTypes.Personnel,
            "product" or "products" or "urun" or "urunler" or "stok" => SlnDataImportTypes.Products,
            _ => value
        };
    }

    private static string? Get(SourceRow row, string key)
    {
        foreach (var alias in Aliases[key])
        {
            var match = row.Values.FirstOrDefault(kv => NormalizeHeader(kv.Key) == NormalizeHeader(alias));
            if (!string.IsNullOrWhiteSpace(match.Key))
                return match.Value;
        }
        return null;
    }

    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fullName"] = ["Ad Soyad", "Musteri Adi", "Müşteri Adı", "Personel Adi", "Personel Adı", "Isim", "İsim", "Name", "Full Name", "FullName"],
        ["phone"] = ["Telefon", "Cep", "GSM", "Phone", "Mobile", "Telefon 1"],
        ["phone2"] = ["Telefon 2", "Phone 2", "GSM 2"],
        ["email"] = ["E-posta", "Email", "E Mail", "Mail"],
        ["birthDate"] = ["Dogum Tarihi", "Doğum Tarihi", "Birth Date", "BirthDate"],
        ["city"] = ["Sehir", "Şehir", "Il", "İl", "City"],
        ["address"] = ["Adres", "Address"],
        ["notes"] = ["Not", "Notlar", "Aciklama", "Açıklama", "Notes"],
        ["branch"] = ["Sube", "Şube", "Branch", "Lokasyon"],
        ["name"] = ["Hizmet Adi", "Hizmet Adı", "Urun Adi", "Ürün Adı", "Ad", "Name"],
        ["category"] = ["Kategori", "Category", "Grup"],
        ["durationMinutes"] = ["Sure Dakika", "Süre Dakika", "Sure", "Süre", "Duration", "DurationMinutes"],
        ["price"] = ["Fiyat", "Price", "Ucret", "Ücret"],
        ["taxRate"] = ["KDV", "Tax", "TaxRate", "Kdv Orani", "KDV Oranı"],
        ["sortOrder"] = ["Sira", "Sıra", "Sort", "SortOrder"],
        ["title"] = ["Unvan", "Title", "Gorev", "Görev"],
        ["role"] = ["Rol", "Role"],
        ["userName"] = ["Kullanici Adi", "Kullanıcı Adı", "Username", "UserName"],
        ["services"] = ["Hizmetler", "Services", "Yetenekler", "Uzmanliklar", "Uzmanlıklar"],
        ["brand"] = ["Marka", "Brand"],
        ["barcode"] = ["Barkod", "Barcode"],
        ["purchasePrice"] = ["Alis Fiyati", "Alış Fiyatı", "Purchase Price", "PurchasePrice", "Maliyet"],
        ["salePrice"] = ["Satis Fiyati", "Satış Fiyatı", "Sale Price", "SalePrice"],
        ["stockQuantity"] = ["Stok", "Stok Miktari", "Stok Miktarı", "Stock", "Quantity"],
        ["minStockLevel"] = ["Kritik Stok", "Minimum Stok", "Min Stock", "MinStockLevel"],
        ["unit"] = ["Birim", "Unit"]
    };

    private static string NormalizeHeader(string value) => NormalizeText(value).Replace(" ", "");

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ');
        }
        return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), "\\s+", " ").Trim();
    }

    private static string ToSlug(string? value)
    {
        var text = NormalizeText(value).Replace(" ", "");
        return Regex.Replace(text, "[^a-z0-9]", "");
    }

    private static string BuildUserName(string fullName, int customerId)
    {
        var slug = ToSlug(fullName);
        if (slug.Length > 18) slug = slug[..18];
        return string.IsNullOrWhiteSpace(slug) ? $"personel{customerId}" : slug;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeEmail(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizePhone(string? value)
        => Regex.Replace(value ?? string.Empty, "\\D", "");

    private static bool IsLikelyEmail(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.Contains('.');

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cultures = new[] { new CultureInfo("tr-TR"), CultureInfo.InvariantCulture };
        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(value, culture, DateTimeStyles.AssumeLocal, out var date))
                return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        }
        return null;
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(CleanNumeric(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static decimal ParseDecimal(string? value, decimal fallback)
    {
        var cleaned = CleanNumeric(value);
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        if (decimal.TryParse(value, NumberStyles.Number, new CultureInfo("tr-TR"), out parsed))
            return parsed;
        return fallback;
    }

    private static string CleanNumeric(string? value)
    {
        var text = (value ?? "").Trim()
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", "", StringComparison.OrdinalIgnoreCase)
            .Replace("₺", "")
            .Replace(" ", "");
        if (text.Contains(',') && text.Contains('.'))
        {
            text = text.LastIndexOf(',') > text.LastIndexOf('.')
                ? text.Replace(".", "").Replace(',', '.')
                : text.Replace(",", "");
        }
        else if (text.Contains(','))
        {
            text = text.Replace(',', '.');
        }
        return text;
    }

    private static List<string> SplitList(string? value)
        => (value ?? "")
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int ResolveSalonRoleId(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized switch
        {
            "mudur" or "manager" => SalonRoles.Ids.Manager,
            "sube muduru" or "branch manager" => SalonRoles.Ids.BranchManager,
            "guzellik uzmani" or "beautician" or "uzman" => SalonRoles.Ids.Beautician,
            "kasiyer" or "cashier" => SalonRoles.Ids.Cashier,
            "resepsiyonist" or "receptionist" => SalonRoles.Ids.Receptionist,
            _ => SalonRoles.Ids.Hairdresser
        };
    }

    private readonly record struct ParsedTable(List<string> Headers, List<SourceRow> Rows);
    private readonly record struct SourceRow(int RowNumber, Dictionary<string, string?> Values);
}
