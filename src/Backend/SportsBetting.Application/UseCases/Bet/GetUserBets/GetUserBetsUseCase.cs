using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public sealed class GetUserBetsUseCase : IGetUserBetsUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IBetReadOnlyRepository _repository;
    private readonly IMapper _mapper;

    public GetUserBetsUseCase(ILoggedUser loggedUser, IBetReadOnlyRepository repository, IMapper mapper)
    {
        _loggedUser = loggedUser;
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<BetResponse>> Execute(GetUserBetsRequest request, CancellationToken cancellationToken)
    {
        var user = await _loggedUser.GetUserAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidLoginException();
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

        BetStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<BetStatus>(request.Status, true, out var status))
        {
            parsedStatus = status;
        }

        var (bets, totalCount) = await _repository.GetPagedByUserIdAsync(
            user.Id,
            pageNumber,
            request.PageSize,
            parsedStatus,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var mappedBets = _mapper.Map<List<BetResponse>>(bets);

        return new PagedResponse<BetResponse>(
            mappedBets,
            totalCount,
            pageNumber,
            request.PageSize);
    }
}
