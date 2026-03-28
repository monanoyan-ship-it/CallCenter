namespace CallCenter.Shared.Entities;

public class SlnRecipeItem
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public SlnRecipe? Recipe { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}
