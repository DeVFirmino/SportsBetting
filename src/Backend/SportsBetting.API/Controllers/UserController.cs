using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Application.UseCases.User.Profile;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;
 
 
[ApiController]
[Route("User")]
public sealed class UserController : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status201Created)]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register(
        [FromServices] IRegisterUserUseCase useCase,
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await useCase.Execute(request, cancellationToken);
        return Created(string.Empty, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [Authorize]

    public async Task<IActionResult> GetUserProfile(
        [FromServices] IGetUserProfileUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);
        
        return Ok(result);
    }
    

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IActionResult> Update(
        [FromServices] IUpdateUserUseCase useCase,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await useCase.Execute(request, cancellationToken);
        
        return NoContent();
    }
    
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse),StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromServices] IChangePasswordUseCase useCase,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await useCase.Execute(request, cancellationToken);
        
        return NoContent();
    }
}
