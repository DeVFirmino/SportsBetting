using Microsoft.AspNetCore.Mvc;
using SportsBetting.API.Attributes;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

public class WalletController : SportsBettingBaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseWalletJson), StatusCodes.Status200OK)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetBalance([FromServices] IGetBalanceUseCase useCase)
    {
        var response = await useCase.Execute();
        
        return Ok(response);
    }

    [HttpPost("deposit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<IActionResult> Deposit([FromServices] IDepositUseCase useCase,
        [FromBody] RequestDepositJson request)
    {
        await useCase.Execute(request);
        
        return NoContent();
    }

}