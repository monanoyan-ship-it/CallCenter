namespace CallCenter.Shared.Entities;

/// <summary>
/// Recetedeki malzeme kalemi. Bir urun + miktar + birim.
/// </summary>
public class SlnRecipeItem
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public SlnRecipe? Recipe { get; set; }

    /// <summary>Kullanilan urun/malzeme</summary>
    public int ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    /// <summary>Miktar (ornegin: 30)</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Birim (gr, ml, adet vb.)</summary>
    public string Unit { get; set; } = "gr";

    /// <summary>Bu kalemin maliyeti (Quantity * urun birim fiyati)</summary>
    public decimal Cost { get; set; }

    /// <summary>Uygulama notu (ornekin: "kok icin", "uc icin")</summary>
    public string? Notes { get; set; }

    public int SortOrder { get; set; }
}
