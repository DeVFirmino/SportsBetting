using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        WalletBalanceResponse response = await useCase.Execute(cancellationToken);

        return Ok(response);
    }

    [HttpPost("deposit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("wallet")]
    public async Task<IActionResult> Deposit(
        [FromServices] IDepositUseCase useCase,
        [FromBody] DepositRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await useCase.Execute(request, idempotencyKey, cancellationToken);

        return NoContent();
    }
}
