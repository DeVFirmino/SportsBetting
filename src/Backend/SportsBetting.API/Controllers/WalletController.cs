using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("Wallet")]
[Authorize]
public sealed class WalletController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(WalletBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(
        [FromServices] IGetBalanceUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.Execute(cancellationToken);
        
        return Ok(response);
    }

    [HttpPost("deposit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deposit(
        [FromServices] IDepositUseCase useCase,
        [FromBody] DepositRequest request,
        CancellationToken cancellationToken)
    {
        await useCase.Execute(request, cancellationToken);
        
        return NoContent();
    }

}
