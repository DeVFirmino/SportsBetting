using Microsoft.AspNetCore.Mvc;
using MyReceipeBook.Communication.Requests;
using MyReceipeBook.Communication.Responses;

namespace MyRecipeBook.API.Controllers;

[Route("[controller]")]
[ApiController] 

public class UserController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status201Created)]
    public IActionResult Register(RequestRegisterUserJson request)

    {
        return Created();
    }
}


