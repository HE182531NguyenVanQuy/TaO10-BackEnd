using System.Net;
using System.Text.Json;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.Exceptions;

namespace TaO10_BackEnd.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware class
    /// </summary>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions and returns appropriate responses
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse response;

        switch (exception)
        {
            case ResourceNotFoundException ex:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 404);
                break;

            case ExamAccessDeniedException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 400);
                break;

            case InvalidAnswerException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 400);
                break;

            case ExamAlreadyCompletedException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 400);
                break;

            case OpenRouterQuotaExceededException ex:
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 429);
                break;

            case OpenRouterUnavailableException ex:
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response = ApiResponse.ErrorResponse(ex.Message, ex.ErrorCode, 503);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = ApiResponse.ErrorResponse("An unexpected error occurred", "INTERNAL_ERROR", 500);
                break;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}
