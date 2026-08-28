using Microsoft.AspNetCore.Mvc;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;
 
[ApiController]
[Route("Login")]
public sealed class LoginController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]

    public async Task<IActionResult> Login(
        [FromServices] IDoLoginUseCase useCase,
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await useCase.Execute(request, cancellationToken);
        
        return Ok(response);
    }
}
