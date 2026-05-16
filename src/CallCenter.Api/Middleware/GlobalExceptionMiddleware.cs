using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex,
                    "Unhandled exception after response started. TraceId={TraceId} Method={Method} Path={Path}",
                    GetTraceId(context),
                    context.Request.Method,
                    context.Request.Path.Value);
                throw;
            }

            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var traceId = GetTraceId(context);
        var (statusCode, title, message, logLevel) = MapException(exception);

        _logger.Log(logLevel, exception,
            "API exception handled. TraceId={TraceId} StatusCode={StatusCode} Method={Method} Path={Path} Query={QueryString} CustomerId={CustomerId} UserId={UserId} BranchId={BranchId}",
            traceId,
            statusCode,
            context.Request.Method,
            context.Request.Path.Value,
            context.Request.QueryString.Value,
            context.User.FindFirst("CustomerId")?.Value,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            context.User.FindFirst("BranchId")?.Value);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = message,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = traceId;
        problem.Extensions["message"] = message;

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().Name;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.Headers["X-Correlation-Id"] = traceId;
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static (int StatusCode, string Title, string Message, LogLevel LogLevel) MapException(Exception exception)
        => exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error", exception.Message, LogLevel.Warning),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message, LogLevel.Warning),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message, LogLevel.Warning),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Business rule error", exception.Message, LogLevel.Warning),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message, LogLevel.Warning),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message, LogLevel.Warning),
            DbUpdateException => (StatusCodes.Status409Conflict, "Database conflict", "Kayit veritabani kisiti nedeniyle kaydedilemedi.", LogLevel.Error),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "Beklenmeyen bir hata olustu.", LogLevel.Error)
        };

    private static string GetTraceId(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        => builder.UseMiddleware<GlobalExceptionMiddleware>();
}
