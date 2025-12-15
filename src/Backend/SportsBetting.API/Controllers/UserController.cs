using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Application.UseCases.User.Register;

namespace SportsBetting.API.Controllers;

[Route("[controller]")]
[ApiController] 

public class UserController : ControllerBase
{
    private readonly RegisterUserUseCase _useCase;
    public UserController(RegisterUserUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisteredUserJson),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromServices] IRegisterUserUseCase useCase,
        [FromBody] RequestRegisterUserJson request)

    {
        var response = await _useCase.Execute(request);
        return Created(string.Empty, response);
    }
}


