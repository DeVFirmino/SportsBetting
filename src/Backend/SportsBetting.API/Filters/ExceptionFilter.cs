using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.API.Filters;

public sealed class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is SportsBettingException exception)
        {
            SetProblem(context, exception.StatusCode, exception.Title, exception.Errors);
            return;
        }

        string correlationId = context.HttpContext.TraceIdentifier;
        _logger.LogError(
            context.Exception,
            "Unhandled exception {CorrelationId} occurred on {Path}",
            correlationId,
            context.HttpContext.Request.Path);

        SetProblem(
            context,
            StatusCodes.Status500InternalServerError,
            "Unexpected error",
            [ResourcesMessagesException.UNKNOWN_ERROR],
            correlationId);
    }

    private static void SetProblem(
        ExceptionContext context,
        int statusCode,
        string title,
        IReadOnlyList<string> errors,
        string? correlationId = null)
    {
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Instance = context.HttpContext.Request.Path,
        };
        problemDetails.Extensions["errors"] = errors;

        if (correlationId is not null)
            problemDetails.Extensions["correlationId"] = correlationId;

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
