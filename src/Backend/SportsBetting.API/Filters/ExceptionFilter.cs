using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SportsBetting.Exceptions;
using SportsBetting.Communication.Responses;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.API.Filters;

public class ExceptionFilter : IExceptionFilter
{

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is SportsBettingException)
            HandleProjectException(context);
        else
            ThrowUnknowException(context);
    }


    private static void HandleProjectException(ExceptionContext context)
    {
        if (context.Exception is InvalidLoginException)
        {
 
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedObjectResult(new ResponseErrorJson(context.Exception.Message));
        }
        
        else if (context.Exception is ErrorOnValidationException exception)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Result = new BadRequestObjectResult(new ResponseErrorJson(exception.ErrorMessage));
        }

    }

    private static void ThrowUnknowException(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Result = new ObjectResult(new ResponseErrorJson(ResourcesMessagesException.UNKNOWN_ERROR));
        }


}
 