using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SportsBetting.Exceptions;
using SportsBetting.Communication.Responses;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.API.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is SportsBettingException)
            HandleProjectException(context);
        else
            ThrowUnknowException(context);
    }


    private void HandleProjectException(ExceptionContext context)
    {
        if (context.Exception is InvalidLoginException)
        {
 
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedObjectResult(new ResponseErrorJson(context.Exception.Message));
        }
        
        else if (context.Exception is ConcurrencyException)
        {
            // The request was valid; another write won the race for the wallet. 409 says
            // "retry", which is what the client should do — a 400 would blame the payload.
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Result = new ConflictObjectResult(new ResponseErrorJson(context.Exception.Message));
        }

        else if (context.Exception is ErrorOnValidationException exception)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Result = new BadRequestObjectResult(new ResponseErrorJson(exception.ErrorMessage));
        }

        else
        {
            // Without this branch an unmapped project exception left the response empty.
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Result = new BadRequestObjectResult(new ResponseErrorJson(context.Exception.Message));
        }
    }

    private void ThrowUnknowException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred: {Message}", context.Exception.Message);
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Result = new ObjectResult(new ResponseErrorJson(ResourcesMessagesException.UNKNOWN_ERROR));
        }


}
 