using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public class GetUserBetsUseCase : IGetUserBetsUseCase
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

    public async Task<ResponsePagedListJson<ResponseBetsJson>> Execute(RequestFilterBetsJson request)
    {
        var user = await _loggedUser.User();

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

        var (bets, totalCount) = await _repository.GetPagedByUserId(
            user.Id,
            pageNumber,
            request.PageSize,
            parsedStatus,
            request.StartDate,
            request.EndDate);

        var mappedBets = _mapper.Map<List<ResponseBetsJson>>(bets);

        return new ResponsePagedListJson<ResponseBetsJson>(
            mappedBets,
            totalCount,
            pageNumber,
            request.PageSize);
    }
}