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
    private readonly IMapper  _mapper;
    private readonly ILoggedUser _loggedUser;

    public GetBetByIdUseCase(IBetReadOnlyRepository repository, IMapper mapper, ILoggedUser loggedUser)
    {
        _repository = repository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }
    
    public async Task<BetResponse> Execute(long id, CancellationToken cancellationToken)
    {
        var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);

        var bet = await _repository.GetByIdAsync(id, cancellationToken);

        if (bet == null || bet.UserId != loggedUser.Id)
        {
            throw new ErrorOnValidationException([ResourcesMessagesException.BET_NOT_FOUND]);
        }

        return _mapper.Map<BetResponse>(bet);
    }

    }
