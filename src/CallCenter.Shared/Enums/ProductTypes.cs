namespace CallCenter.Shared.Enums;

/// <summary>
/// Musteri (firma) urun tipi. Hangi urunleri kullaniyor.
/// CustomerProduct.ProductTypeId ile iliskilendirilir (bire-cok).
/// </summary>
public static class ProductTypes
{
    public static readonly TypeItem CallCenter = new(1, "CallCenter", "ProductType.CallCenter", "Call Center", "bi-headset", "bg-primary", 1);
    public static readonly TypeItem Salon = new(2, "Salon", "ProductType.Salon", "Salon Yönetimi", "bi-scissors", "bg-warning text-dark", 2);

    public static IEnumerable<TypeItem> All => new[] { CallCenter, Salon };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CallCenter = 1;
        public const int Salon = 2;
    }
}
