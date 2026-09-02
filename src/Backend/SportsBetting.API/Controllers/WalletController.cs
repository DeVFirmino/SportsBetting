using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("wallet")]
[Authorize]
public sealed class WalletController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(WalletBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(
        [FromServices] IGetBalanceUseCase useCase,
        CancellationToken cancellationToken)
    {
        WalletBalanceResponse response = await useCase.Execute(cancellationToken);

        return Ok(response);
    }

    [HttpPost("deposits")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deposit(
        [FromServices] IDepositUseCase useCase,
        [FromBody] DepositRequest request,
        CancellationToken cancellationToken)
    {
        await useCase.Execute(request, cancellationToken);

        return NoContent();
    }
}
