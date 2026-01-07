using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using SportsBetting.API.Filters;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.API.Attributes;

public class AuthenticatedUserAttribute : TypeFilterAttribute 
{
    public AuthenticatedUserAttribute() : base(typeof(AuthenticatedUserFilter))
    {
        
    }
}