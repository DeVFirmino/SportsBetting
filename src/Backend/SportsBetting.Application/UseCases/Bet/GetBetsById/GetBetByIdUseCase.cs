using AutoMapper;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.GetBetsById;

public sealed class GetBetByIdUseCase : IGetBetByIdUseCase
{
    private readonly IBetReadOnlyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public GetBetByIdUseCase(
        IBetReadOnlyRepository repository,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<BetResponse> Execute(long id, CancellationToken cancellationToken)
    {
        Domain.Entities.User user = await _loggedUser.GetUserAsync(cancellationToken);
        Domain.Entities.Bet? bet = await _repository.GetByIdAsync(id, user.Id, cancellationToken);

        if (bet is null)
            throw new ResourceNotFoundException(ResourcesMessagesException.BET_NOT_FOUND);

        return _mapper.Map<BetResponse>(bet);
    }
}
