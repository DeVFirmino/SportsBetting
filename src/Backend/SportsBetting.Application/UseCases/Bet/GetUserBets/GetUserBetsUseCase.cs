using AutoMapper;
using SportsBetting.Communication.Responses;
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
    
    public async Task<List<ResponseBetsJson>> Execute()
    {
        var user = await _loggedUser.User();
        
        if (user is null)
        {
            throw new InvalidLoginException();
        }
 
        var bets = await _repository.GetByUserId(user.Id);
        
        return _mapper.Map<List<ResponseBetsJson>>(bets);
    }
}