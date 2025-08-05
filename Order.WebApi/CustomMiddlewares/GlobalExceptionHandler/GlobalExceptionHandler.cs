using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Order.Core.Exceptions;

namespace Order.WebApi.CustomMiddlewares.GlobalExceptionHandler;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
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
            await HandleExceptionAsync(context, ex);
        }
    }
    

    public async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = (string)GetOrCreateCorrelationId(context);
        
        // Logging with context
        _logger.LogError(exception,
            "Unhandled exception occurred.CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, UserId: {UserId}",
            correlationId,
            context.Request.Path,
            context.Request.Method,
            GetUserId(context));

        var response = CreateErrorResponse(exception, correlationId);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;
        
        context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            ValidationException validationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = validationException.Message,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },

            ArgumentException argEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = "Invalid request parameters",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },

            KeyNotFoundException keyEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = "The requested resource was not found",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },

            TimeoutException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Title = "Request Timeout",
                Detail = "The request timed out",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },

            TaskCanceledException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Title = "Request Cancelled",
                Detail = "The request was cancelled",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },

            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
            }
        };
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("id")?.Value ?? context.User?.Identity?.Name;
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }

        if (!string.IsNullOrEmpty(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }
        
        return Guid.NewGuid().ToString();
    }
}