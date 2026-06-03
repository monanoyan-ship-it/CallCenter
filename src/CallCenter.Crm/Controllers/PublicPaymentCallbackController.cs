using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class PublicPaymentCallbackController : Controller
{
    [HttpPost("api/payments/iyzico-callback")]
    public Task<IActionResult> IyzicoCallback()
        => ForwardPaymentPostAsync("api/payments/iyzico-callback");

    [HttpPost("api/payments/iyzico-webhook")]
    public Task<IActionResult> IyzicoWebhook()
        => ForwardPaymentPostAsync("api/payments/iyzico-webhook");

    private async Task<IActionResult> ForwardPaymentPostAsync(string apiPath)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("CrmApi");

        using var request = new HttpRequestMessage(HttpMethod.Post, apiPath);
        request.Content = new StreamContent(Request.Body);
        if (!string.IsNullOrWhiteSpace(Request.ContentType))
            request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/html; charset=utf-8";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType
        };
    }
}
