using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;

namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public sealed class GetUserBetsUseCase : IGetUserBetsUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IBetReadOnlyRepository _repository;
    private readonly IMapper _mapper;

    public GetUserBetsUseCase(
        ILoggedUser loggedUser,
        IBetReadOnlyRepository repository,
        IMapper mapper)
    {
        _loggedUser = loggedUser;
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<BetResponse>> Execute(
        GetUserBetsRequest request,
        CancellationToken cancellationToken)
    {
        Domain.Entities.User user = await _loggedUser.GetUserAsync(cancellationToken);
        int pageNumber = Math.Max(request.PageNumber, 1);

        (List<Domain.Entities.Bet> bets, int totalCount) = await _repository.GetPagedByUserIdAsync(
            user.Id,
            pageNumber,
            request.PageSize,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return new PagedResponse<BetResponse>(
            _mapper.Map<List<BetResponse>>(bets),
            totalCount,
            pageNumber,
            request.PageSize);
    }
}
