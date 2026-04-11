namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteriye uygulanan recete kaydi (tarihce).
/// Bir recete sablonuna referans verebilir veya serbest metin olabilir.
/// </summary>
public class SlnFormula
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    /// <summary>Kullanilan recete sablonu (opsiyonel)</summary>
    public int? RecipeId { get; set; }
    public SlnRecipe? Recipe { get; set; }

    /// <summary>Serbest metin formul (recete yoksa veya ek notlar)</summary>
    public string FormulaText { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? OxidantRatio { get; set; }
    public string? ApplicationNotes { get; set; }

    /// <summary>Uygulanan hizmet</summary>
    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int? AppliedByPersonnelId { get; set; }
    public CustomerPersonnel? AppliedByPersonnel { get; set; }

    /// <summary>Toplam malzeme maliyeti (receteden veya manuel)</summary>
    public decimal MaterialCost { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
