using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.API.Filters;

public sealed class AuthenticatedUserFilter : IAsyncAuthorizationFilter
{
    private readonly IAccessTokenValidator _accessTokenValidator;
    private readonly IUserReadOnlyRepository _repository;

    public AuthenticatedUserFilter(IAccessTokenValidator accessTokenValidator,  IUserReadOnlyRepository repository)
    {
        _accessTokenValidator = accessTokenValidator;
        _repository = repository;
            
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var token = TokenOnRequest(context);

            var userIdentifier = _accessTokenValidator.ValidateAndGetUserIdentifier(token);
        
            var exist = await _repository.ExistsActiveUserWithIdentifierAsync(
                userIdentifier,
                context.HttpContext.RequestAborted);
            if (exist == false)
            {
                throw new SportsBettingException(ResourcesMessagesException.NO_PERMISSION);
            }
        }
        catch (SecurityTokenExpiredException)
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse("TokenIsExpired")
            {
                TokenIsExpired = true,
            });
        }
        catch (SportsBettingException ex)
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse(ex.Message));
        }
        catch (SecurityTokenException)
        {
            SetUnauthorized(context);
        }
        catch (InvalidOperationException)
        {
            SetUnauthorized(context);
        }
        catch (FormatException)
        {
            SetUnauthorized(context);
        }
        catch (ArgumentException)
        {
            SetUnauthorized(context);
        }
    }

    
    private static string TokenOnRequest(AuthorizationFilterContext context)
    {
        var authentication = context.HttpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authentication))
        {
            throw new SportsBettingException(ResourcesMessagesException.NO_TOKEN);
        }

        return authentication["Bearer".Length..].Trim();
    }

    private static void SetUnauthorized(AuthorizationFilterContext context)
    {
        context.Result = new UnauthorizedObjectResult(
            new ErrorResponse(ResourcesMessagesException.NO_PERMISSION));
    }
}
