using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Abstractions.Errors;
using Platform.Domain.Common;

namespace Platform.Api.Infrastructure.ErrorHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return false;
        }

        var (statusCode, title, type) = exception switch
        {
            RequestValidationException => (StatusCodes.Status400BadRequest, "Request validation failed.", "https://httpstatuses.com/400"),
            ConflictException => (StatusCodes.Status409Conflict, "The request conflicted with the current state.", "https://httpstatuses.com/409"),
            ConcurrencyException => (StatusCodes.Status409Conflict, "The resource was modified by another operation.", "https://httpstatuses.com/409"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "https://httpstatuses.com/500")
        };

        httpContext.Response.StatusCode = statusCode;

        if (exception is RequestValidationException validationException)
        {
            var problem = new HttpValidationProblemDetails(validationException.Errors)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
