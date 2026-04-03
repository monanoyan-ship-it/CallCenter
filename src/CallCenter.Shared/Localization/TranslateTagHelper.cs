using CallCenter.Shared.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CallCenter.Shared.Localization;

/// <summary>
/// Razor view'lerde kolay ceviri icin tag helper.
/// Kullanim: &lt;t key="landing.hero.title"&gt;Fallback text&lt;/t&gt;
/// </summary>
[HtmlTargetElement("t", Attributes = "key")]
public class TranslateTagHelper : TagHelper
{
    private readonly ITranslationService _translation;

    public TranslateTagHelper(ITranslationService translation)
    {
        _translation = translation;
    }

    [HtmlAttributeName("key")]
    public string Key { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // <t> tagini renderlamaz, sadece icerigi gosterir

        var childContent = await output.GetChildContentAsync();
        var fallback = childContent.GetContent()?.Trim();

        var translated = _translation.T(Key, fallback);
        output.Content.SetContent(translated);
    }
}
