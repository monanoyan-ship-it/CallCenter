using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

internal static class ProxyResultHelper
{
    private const string JsonContentType = "application/json; charset=utf-8";

    public static async Task<IActionResult> ToApiResult(HttpResponseMessage response, HttpContext httpContext)
    {
        var statusCode = (int)response.StatusCode;

        if (statusCode == StatusCodes.Status204NoContent)
            return new StatusCodeResult(StatusCodes.Status204NoContent);

        CopyForwardedHeaders(response, httpContext);

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        var contentType = response.Content.Headers.ContentType?.ToString();
        var isJson = string.IsNullOrWhiteSpace(mediaType)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            var correlationId = GetHeaderValue(response, "X-Correlation-Id");
            return new ContentResult
            {
                Content = BuildErrorJson(text, statusCode, correlationId),
                ContentType = JsonContentType,
                StatusCode = statusCode
            };
        }

        if (isJson)
        {
            var text = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = string.IsNullOrWhiteSpace(text) ? "null" : text,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? JsonContentType : contentType,
                StatusCode = statusCode
            };
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        fileName = fileName?.Trim('"');

        var result = new FileContentResult(bytes,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        if (!string.IsNullOrWhiteSpace(fileName))
            result.FileDownloadName = fileName;

        return result;
    }

    private static string BuildErrorJson(string content, int statusCode, string? correlationId)
    {
        var (message, traceId) = ExtractError(content);
        message = SanitizeMessage(message) ?? DefaultMessage(statusCode);
        traceId = string.IsNullOrWhiteSpace(traceId) ? correlationId : traceId;

        var payload = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["status"] = statusCode
        };

        if (!string.IsNullOrWhiteSpace(traceId))
            payload["traceId"] = traceId;

        return JsonSerializer.Serialize(payload);
    }

    private static (string? Message, string? TraceId) ExtractError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (null, null);

        var text = content.Trim();
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
                return (root.GetString(), null);

            if (root.ValueKind != JsonValueKind.Object)
                return (text, null);

            var traceId = TryReadString(root, "traceId")
                ?? TryReadString(root, "correlationId");
            var message = TryReadString(root, "message")
                ?? TryReadString(root, "error")
                ?? TryReadString(root, "detail")
                ?? TryReadString(root, "title")
                ?? TryReadValidationError(root);

            return (message, traceId);
        }
        catch (JsonException)
        {
            return (text, null);
        }
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? TryReadValidationError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors))
            return null;

        if (errors.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in errors.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
            }
            return null;
        }

        if (errors.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in errors.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();

            if (property.Value.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in property.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
            }
        }

        return null;
    }

    private static string? SanitizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var cleaned = message.Trim().Trim('"');
        if (cleaned.StartsWith("<", StringComparison.Ordinal))
            return null;

        return cleaned.Length > 1000 ? cleaned[..1000] : cleaned;
    }

    private static string DefaultMessage(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status401Unauthorized => "Oturum suresi doldu. Yeniden giris yapin.",
            StatusCodes.Status403Forbidden => "Bu islem icin yetkiniz yok.",
            StatusCodes.Status404NotFound => "Kayit bulunamadi.",
            StatusCodes.Status409Conflict => "Kayit cakismasi nedeniyle islem tamamlanamadi.",
            _ => statusCode >= 500 ? "Beklenmeyen bir hata olustu." : "Bir hata olustu."
        };

    private static void CopyForwardedHeaders(HttpResponseMessage response, HttpContext httpContext)
    {
        CopyHeader(response, httpContext, "Content-Disposition");
        CopyHeader(response, httpContext, "X-Correlation-Id");
    }

    private static void CopyHeader(HttpResponseMessage response, HttpContext httpContext, string headerName)
    {
        var value = GetHeaderValue(response, headerName);
        if (!string.IsNullOrWhiteSpace(value))
            httpContext.Response.Headers[headerName] = value;
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
            return values.FirstOrDefault();

        if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
            return contentValues.FirstOrDefault();

        return null;
    }
}
