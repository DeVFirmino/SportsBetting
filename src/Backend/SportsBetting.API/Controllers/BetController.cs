using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("bets")]
[Authorize]
public sealed class BetController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(BetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PlaceBet(
        [FromServices] IPlaceBetUseCase useCase,
        [FromBody] PlaceBetRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        BetResponse result = await useCase.Execute(request, idempotencyKey, cancellationToken);

        return CreatedAtAction(nameof(GetBetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBets(
        [FromServices] IGetUserBetsUseCase useCase,
        [FromQuery] GetUserBetsRequest request,
        CancellationToken cancellationToken)
    {
        PagedResponse<BetResponse> result = await useCase.Execute(request, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBetById(
        [FromServices] IGetBetByIdUseCase useCase,
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        BetResponse result = await useCase.Execute(id, cancellationToken);

        return Ok(result);
    }
}
