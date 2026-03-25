namespace CallCenter.Shared.Enums;

/// <summary>
/// Musteri (firma) urun tipi. Hangi urunleri kullaniyor.
/// Customer.ProductTypeId ile iliskilendirilir.
/// </summary>
public static class ProductTypes
{
    public static readonly TypeItem CallCenter = new(1, "CallCenter", "ProductType.CallCenter", "Call Center", "bi-headset", "bg-primary", 1);
    public static readonly TypeItem Salon = new(2, "Salon", "ProductType.Salon", "Salon Yonetimi", "bi-scissors", "bg-purple", 2);
    public static readonly TypeItem Both = new(3, "Both", "ProductType.Both", "Call Center + Salon", "bi-grid-fill", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { CallCenter, Salon, Both };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CallCenter = 1;
        public const int Salon = 2;
        public const int Both = 3;
    }
}
