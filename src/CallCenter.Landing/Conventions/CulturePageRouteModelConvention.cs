using CallCenter.Shared.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CallCenter.Landing.Conventions;

/// <summary>
/// Her Razor Page icin {culture}/path seklinde ek route selector ekler.
/// Ornek: /call-center -> hem /call-center hem /{culture}/call-center
/// </summary>
public class CulturePageRouteModelConvention : IPageRouteModelConvention
{
    public void Apply(PageRouteModel model)
    {
        var originalSelectors = model.Selectors.ToList();

        foreach (var selector in originalSelectors)
        {
            var template = selector.AttributeRouteModel?.Template;

            // Bos template (Index sayfasi) -> {culture}
            // Dolu template (call-center) -> {culture}/call-center
            var cultureTemplate = string.IsNullOrEmpty(template)
                ? "{culture}"
                : $"{{culture}}/{template}";

            model.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = cultureTemplate
                }
            });
        }
    }
}
