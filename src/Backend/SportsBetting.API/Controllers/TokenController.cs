using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("tokens")]
public sealed class TokenController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("tokens")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(
        [FromServices] IDoLoginUseCase useCase,
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthenticatedUserResponse response = await useCase.Execute(request, cancellationToken);
        return Ok(response);
    }
}
