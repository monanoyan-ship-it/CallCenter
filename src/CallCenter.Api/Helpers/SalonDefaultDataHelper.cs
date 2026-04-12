using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
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
        IUnitOfWork uow,
        int customerId)
    {
        // Zaten hizmet kategorisi varsa atla (tekrar calismasin)
        var hasCategories = await serviceCategoryEs.GetAllQueryable().AnyAsync(c => c.CustomerId == customerId);
        if (hasCategories) return;

        // ═══ Hizmet Kategorileri + Hizmetler ═══
        var categories = GetDefaultCategories();

        foreach (var cat in categories)
        {
            var category = new SlnServiceCategory
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

            var sortOrder = 1;
            foreach (var svc in cat.Services)
            {
                serviceEs.Add(new SlnService
                {
                    CustomerId = customerId,
                    CategoryId = category.Id,
                    Name = svc.Name,
                    Price = svc.Price,
                    DurationMinutes = svc.Duration,
                    SortOrder = sortOrder++,
                    IsActive = true
                });
            }
            await uow.SaveChangesAsync();
        }

        // ═══ Masraf Kategorileri ═══
        var expenseCategories = new[]
        {
            "Kira", "Faturalar", "Malzeme / Urun Alimi", "Personel Giderleri",
            "Vergi / SGK", "Temizlik", "Bakim / Onarim", "Reklam / Pazarlama", "Diger"
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

        // ═══ Ana Kasa ═══
        var hasRegister = await cashRegisterEs.GetAllQueryable().AnyAsync(r => r.CustomerId == customerId);
        if (!hasRegister)
        {
            cashRegisterEs.Add(new SlnCashRegister
            {
                CustomerId = customerId,
                Name = "Ana Kasa",
                IsActive = true
            });
            await uow.SaveChangesAsync();
        }
    }

    /// <summary>AppDbContext ile (Program.cs seed'den cagirmak icin)</summary>
    public static async Task SeedDefaultDataAsync(AppDbContext db, int customerId)
    {
        // Zaten hizmet kategorisi varsa atla (tekrar calismasin)
        var hasCategories = await db.SlnServiceCategories.AnyAsync(c => c.CustomerId == customerId);
        if (hasCategories) return;

        // ═══ Hizmet Kategorileri + Hizmetler ═══
        var categories = GetDefaultCategories();

        foreach (var cat in categories)
        {
            var category = new SlnServiceCategory
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

            var sortOrder = 1;
            foreach (var svc in cat.Services)
            {
                db.SlnServices.Add(new SlnService
                {
                    CustomerId = customerId,
                    CategoryId = category.Id,
                    Name = svc.Name,
                    Price = svc.Price,
                    DurationMinutes = svc.Duration,
                    SortOrder = sortOrder++,
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();
        }

        // ═══ Masraf Kategorileri ═══
        var expenseCategories = new[]
        {
            "Kira", "Faturalar", "Malzeme / Urun Alimi", "Personel Giderleri",
            "Vergi / SGK", "Temizlik", "Bakim / Onarim", "Reklam / Pazarlama", "Diger"
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

        // ═══ Ana Kasa ═══
        var hasRegister = await db.SlnCashRegisters.AnyAsync(r => r.CustomerId == customerId);
        if (!hasRegister)
        {
            db.SlnCashRegisters.Add(new SlnCashRegister
            {
                CustomerId = customerId,
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
            ("Sac", "bi-scissors", "#7b1fa2", 1, new[]
            {
                ("Sac Kesim", 150m, 30),
                ("Fon", 100m, 20),
                ("Sac Boyama", 400m, 90),
                ("Ombre / Balyaj", 600m, 120),
                ("Keratin Bakimi", 500m, 90),
                ("Sac Bakimi", 200m, 45),
                ("Sac Acma", 300m, 60),
                ("Gecici Duzlestirme", 250m, 60),
            }),
            ("Cilt Bakimi", "bi-droplet", "#e91e63", 2, new[]
            {
                ("Cilt Bakimi", 300m, 60),
                ("Hydrafacial", 500m, 45),
                ("Cilt Temizleme", 200m, 30),
                ("Maske Uygulamasi", 150m, 20),
            }),
            ("Tirnak", "bi-brush", "#f44336", 3, new[]
            {
                ("Manikur", 150m, 30),
                ("Pedikur", 200m, 45),
                ("Protez Tirnak", 400m, 90),
                ("Kalici Oje", 200m, 45),
                ("Tirnak Bakimi", 100m, 20),
            }),
            ("Makyaj", "bi-palette", "#ff9800", 4, new[]
            {
                ("Gunluk Makyaj", 300m, 45),
                ("Gelin Makyaji", 1000m, 90),
                ("Ozel Gun Makyaji", 500m, 60),
            }),
            ("Agda / Epilasyon", "bi-stars", "#4caf50", 5, new[]
            {
                ("Tum Vucut Agda", 400m, 60),
                ("Bacak Agda", 150m, 20),
                ("Kol Agda", 100m, 15),
                ("Yuz Agda", 50m, 10),
                ("Bikini Agda", 100m, 15),
            }),
            ("Ozel Bakim", "bi-gem", "#2196f3", 6, new[]
            {
                ("Kas Alimi", 50m, 15),
                ("Kirpik Eki", 300m, 60),
                ("Kirpik Lifting", 200m, 30),
                ("Kalici Kas", 800m, 90),
            }),
        };
    }
}
