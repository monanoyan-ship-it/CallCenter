using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Helpers;

/// <summary>
/// Yeni salon kayit olunca veya mevcut salona default data eklemek icin.
/// </summary>
public static class SalonDefaultDataHelper
{
    /// <summary>Entity services ile (Factory'lerden cagirmak icin)</summary>
    public static async Task SeedDefaultDataAsync(
        ISlnServiceCategoryEntityService serviceCategoryEs,
        ISlnServiceEntityService serviceEs,
        ISlnExpenseCategoryEntityService expenseCategoryEs,
        ISlnCashRegisterEntityService cashRegisterEs,
        ICustomerPortalModuleEntityService portalModuleEs,
        ISlnBranchEntityService branchEs,
        IUnitOfWork uow,
        int customerId)
    {
        // ═══ Default Moduller ═══
        var existingModules = await portalModuleEs.GetAllQueryable()
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();

        foreach (var mod in SalonPortalModules.Defaults)
        {
            var existing = existingModules.FirstOrDefault(m => m.ModuleId == mod.Id);
            if (existing == null)
            {
                portalModuleEs.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = mod.Id,
                    IsActive = true
                });
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.ActivatedAt = DateTime.UtcNow;
                existing.DeactivatedAt = null;
            }
        }
        await uow.SaveChangesAsync();

        // Zaten hizmet kategorisi varsa atla (tekrar calismasin)
        var existingCategories = await serviceCategoryEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        // ═══ Hizmet Kategorileri + Hizmetler ═══
        var categories = GetDefaultCategories();

        foreach (var cat in categories)
        {
            var category = existingCategories.FirstOrDefault(c => c.Name == cat.Name);
            if (category == null)
            {
                category = new SlnServiceCategory
                {
                    CustomerId = customerId,
                    Name = cat.Name,
                    IconClass = cat.Icon,
                    Color = cat.Color,
                    SortOrder = cat.Sort,
                    IsActive = true
                };
                serviceCategoryEs.Add(category);
                await uow.SaveChangesAsync();
                existingCategories.Add(category);
            }

            var existingServices = await serviceEs.GetAllQueryable()
                .Where(s => s.CustomerId == customerId && s.CategoryId == category.Id)
                .ToListAsync();
            var sortOrder = existingServices.Select(s => s.SortOrder).DefaultIfEmpty(0).Max();
            foreach (var svc in cat.Services)
            {
                if (existingServices.Any(s => s.Name == svc.Name)) continue;

                serviceEs.Add(new SlnService
                {
                    CustomerId = customerId,
                    CategoryId = category.Id,
                    Name = svc.Name,
                    Price = svc.Price,
                    DurationMinutes = svc.Duration,
                    SortOrder = ++sortOrder,
                    IsActive = true
                });
            }
            await uow.SaveChangesAsync();
        }

        // ═══ Masraf Kategorileri ═══
        var expenseCategories = new[]
        {
            "Kira", "Faturalar", "Malzeme / Ürün Alımı", "Personel Giderleri",
            "Vergi / SGK", "Temizlik", "Bakım / Onarım", "Reklam / Pazarlama", "Diğer"
        };

        foreach (var name in expenseCategories)
        {
            expenseCategoryEs.Add(new SlnExpenseCategory
            {
                CustomerId = customerId,
                Name = name,
                IsSystem = true
            });
        }
        await uow.SaveChangesAsync();

        // ═══ Ana Kasa (merkez subeye bagli) ═══
        var hasRegister = await cashRegisterEs.GetAllQueryable().AnyAsync(r => r.CustomerId == customerId);
        if (!hasRegister)
        {
            var hqBranch = await branchEs.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

            cashRegisterEs.Add(new SlnCashRegister
            {
                CustomerId = customerId,
                BranchId = hqBranch?.Id,
                Name = "Ana Kasa",
                IsActive = true
            });
            await uow.SaveChangesAsync();
        }
    }

    /// <summary>AppDbContext ile (Program.cs seed'den cagirmak icin)</summary>
    public static async Task SeedDefaultDataAsync(AppDbContext db, int customerId)
    {
        // ═══ Default Moduller ═══
        var existingModules = await db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();

        foreach (var mod in SalonPortalModules.Defaults)
        {
            var existing = existingModules.FirstOrDefault(m => m.ModuleId == mod.Id);
            if (existing == null)
            {
                db.CustomerPortalModules.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = mod.Id,
                    IsActive = true
                });
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.ActivatedAt = DateTime.UtcNow;
                existing.DeactivatedAt = null;
            }
        }
        await db.SaveChangesAsync();

        // Zaten hizmet kategorisi varsa atla (tekrar calismasin)
        var existingCategories = await db.SlnServiceCategories
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        // ═══ Hizmet Kategorileri + Hizmetler ═══
        var categories = GetDefaultCategories();

        foreach (var cat in categories)
        {
            var category = existingCategories.FirstOrDefault(c => c.Name == cat.Name);
            if (category == null)
            {
                category = new SlnServiceCategory
                {
                    CustomerId = customerId,
                    Name = cat.Name,
                    IconClass = cat.Icon,
                    Color = cat.Color,
                    SortOrder = cat.Sort,
                    IsActive = true
                };
                db.SlnServiceCategories.Add(category);
                await db.SaveChangesAsync();
                existingCategories.Add(category);
            }

            var existingServices = await db.SlnServices
                .Where(s => s.CustomerId == customerId && s.CategoryId == category.Id)
                .ToListAsync();
            var sortOrder = existingServices.Select(s => s.SortOrder).DefaultIfEmpty(0).Max();
            foreach (var svc in cat.Services)
            {
                if (existingServices.Any(s => s.Name == svc.Name)) continue;

                db.SlnServices.Add(new SlnService
                {
                    CustomerId = customerId,
                    CategoryId = category.Id,
                    Name = svc.Name,
                    Price = svc.Price,
                    DurationMinutes = svc.Duration,
                    SortOrder = ++sortOrder,
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();
        }

        // ═══ Masraf Kategorileri ═══
        var expenseCategories = new[]
        {
            "Kira", "Faturalar", "Malzeme / Ürün Alımı", "Personel Giderleri",
            "Vergi / SGK", "Temizlik", "Bakım / Onarım", "Reklam / Pazarlama", "Diğer"
        };

        foreach (var name in expenseCategories)
        {
            db.SlnExpenseCategories.Add(new SlnExpenseCategory
            {
                CustomerId = customerId,
                Name = name,
                IsSystem = true
            });
        }
        await db.SaveChangesAsync();

        // ═══ Ana Kasa (merkez subeye bagli) ═══
        var hasRegister = await db.SlnCashRegisters.AnyAsync(r => r.CustomerId == customerId);
        if (!hasRegister)
        {
            var hqBranch = await db.SlnBranches
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

            db.SlnCashRegisters.Add(new SlnCashRegister
            {
                CustomerId = customerId,
                BranchId = hqBranch?.Id,
                Name = "Ana Kasa",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    private static (string Name, string Icon, string Color, int Sort, (string Name, decimal Price, int Duration)[] Services)[] GetDefaultCategories()
    {
        return new (string Name, string Icon, string Color, int Sort, (string Name, decimal Price, int Duration)[] Services)[]
        {
            ("Saç", "bi-scissors", "#7b1fa2", 1, new[]
            {
                ("Saç Kesim", 150m, 30),
                ("Fon", 100m, 20),
                ("Saç Boyama", 400m, 90),
                ("Ombre / Balyaj", 600m, 120),
                ("Keratin Bakımı", 500m, 90),
                ("Saç Bakımı", 200m, 45),
                ("Saç Açma", 300m, 60),
                ("Geçici Düzleştirme", 250m, 60),
            }),
            ("Cilt Bakımı", "bi-droplet", "#e91e63", 2, new[]
            {
                ("Cilt Bakımı", 300m, 60),
                ("Hydrafacial", 500m, 45),
                ("Cilt Temizleme", 200m, 30),
                ("Maske Uygulaması", 150m, 20),
            }),
            ("Tırnak", "bi-brush", "#f44336", 3, new[]
            {
                ("Manikür", 150m, 30),
                ("Pedikür", 200m, 45),
                ("Protez Tırnak", 400m, 90),
                ("Kalıcı Oje", 200m, 45),
                ("Tırnak Bakımı", 100m, 20),
            }),
            ("Makyaj", "bi-palette", "#ff9800", 4, new[]
            {
                ("Günlük Makyaj", 300m, 45),
                ("Gelin Makyajı", 1000m, 90),
                ("Özel Gün Makyajı", 500m, 60),
            }),
            ("Ağda / Epilasyon", "bi-stars", "#4caf50", 5, new[]
            {
                ("Lazer Epilasyon", 750m, 45),
                ("Tüm Vücut Lazer Epilasyon", 2500m, 90),
                ("Bölgesel Lazer Epilasyon", 600m, 30),
                ("Tüm Vücut Ağda", 400m, 60),
                ("Bacak Ağda", 150m, 20),
                ("Kol Ağda", 100m, 15),
                ("Yüz Ağda", 50m, 10),
                ("Bikini Ağda", 100m, 15),
            }),
            ("Özel Bakım", "bi-gem", "#2196f3", 6, new[]
            {
                ("Kaş Alımı", 50m, 15),
                ("Kirpik Eki", 300m, 60),
                ("Kirpik Lifting", 200m, 30),
                ("Kalıcı Kaş", 800m, 90),
            }),
        };
    }
}
