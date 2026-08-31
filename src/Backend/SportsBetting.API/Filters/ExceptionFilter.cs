using System.Net;
using Polly.CircuitBreaker;
using Polly.Timeout;
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
        if (context.Exception is SportsBettingException)
        {
            HandleProjectException(context);
            return;
        }

        // A null status is a network-level failure — DNS, refused connection — from the only
        // HTTP dependency this API has; it is as much "upstream unavailable" as an explicit 503.
        if (context.Exception is HttpRequestException httpRequestException
            && httpRequestException.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or null)
        {
            HandleUpstreamException(context, httpRequestException);
            return;
        }

        // The resilience pipeline in front of API-Football stops calling a failing upstream and
        // gives up on one that is too slow. Neither is the caller's fault, so both answer 503
        // rather than surfacing as an unexplained 500.
        if (context.Exception is BrokenCircuitException or TimeoutRejectedException)
        {
            HandleUnavailableUpstream(context, context.Exception);
            return;
        }

        HandleUnknownException(context);
    }

    private static void HandleProjectException(ExceptionContext context)
    {
        if (context.Exception is InvalidLoginException)
        {
            SetProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", [context.Exception.Message]);
            return;
        }

        if (context.Exception is ConcurrencyException)
        {
            SetProblem(context, StatusCodes.Status409Conflict, "Conflict", [context.Exception.Message]);
            return;
        }

        if (context.Exception is ErrorOnValidationException exception)
        {
            SetProblem(context, StatusCodes.Status400BadRequest, "Validation failed", exception.ErrorMessage);
            return;
        }

        SetProblem(context, StatusCodes.Status400BadRequest, "Bad request", [context.Exception.Message]);
    }

    private void HandleUnavailableUpstream(ExceptionContext context, Exception exception)
    {
        _logger.LogWarning(exception, "Upstream API-Football is not reachable");

        SetProblem(
            context,
            StatusCodes.Status503ServiceUnavailable,
            "Upstream service unavailable",
            [ResourcesMessagesException.UNKNOWN_ERROR]);
    }

    private void HandleUpstreamException(ExceptionContext context, HttpRequestException exception)
    {
        int statusCode = exception.StatusCode is HttpStatusCode.BadGateway
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status503ServiceUnavailable;

        _logger.LogWarning(exception, "Upstream API-Football request failed with {StatusCode}", statusCode);

        SetProblem(context, statusCode, "Upstream service unavailable", [exception.Message]);
    }

    private void HandleUnknownException(ExceptionContext context)
    {
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
        IList<string> errors,
        string? correlationId = null)
    {
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Instance = context.HttpContext.Request.Path,
        };
        problemDetails.Extensions["errors"] = errors;

        if (string.IsNullOrWhiteSpace(correlationId) is false)
            problemDetails.Extensions["correlationId"] = correlationId;

        context.HttpContext.Response.StatusCode = statusCode;
        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
        };
    }
}
