using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public class PlaceBetUseCase : IPlaceBetUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IMapper _mapper;
    private readonly IBetWriteOnlyRepository _betWriteOnlyRepository; 
    private readonly IWalletUpdateOnlyRepository _walletUpdateOnlyRepository;
    private readonly IWalletReadOnlyRepository _walletReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFootballApiService _footballApiService;

    public PlaceBetUseCase(
        ILoggedUser loggedUser,
        IMapper mapper,
        IBetWriteOnlyRepository betWriteOnlyRepository,
        IWalletUpdateOnlyRepository walletUpdateOnlyRepository,
        IWalletReadOnlyRepository walletReadOnlyRepository,
        IUnitOfWork unitOfWork,
        IFootballApiService footballApiService)
    {
        _loggedUser = loggedUser;
        _mapper = mapper;
        _betWriteOnlyRepository = betWriteOnlyRepository;
        _walletUpdateOnlyRepository = walletUpdateOnlyRepository;
        _walletReadOnlyRepository = walletReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _footballApiService = footballApiService;
    }
    
    
    
    public async Task<ResponseBetsJson> Execute(RequestPlaceBetJson request)
    {
        await Validate(request);
         
        var loggedUser = await _loggedUser.User();
        
        var wallet = await _walletReadOnlyRepository.GetByUserId(loggedUser.Id);
        
        //3 busca carteira
        if (wallet is null) 
            throw new ErrorOnValidationException([ResourcesMessagesException.WALLET_NOT_FOUND]);
        
        if (wallet.Balance < request.Amount)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);
        
        // Valida se o fixture existe
        var fixtures = await _footballApiService.GetUpcomingFixtures();
        
        
        var fixtureExists = fixtures.Any(f => f.FixtureId == request.FixtureId);

        if (!fixtureExists)
            throw new ErrorOnValidationException([ResourcesMessagesException.FIXTURE_NOT_FOUND]);
        
        var bet = _mapper.Map<Domain.Entities.Bet>(request);
        bet.UserId = loggedUser.Id;
        bet.Status = BetStatus.Pending;
        bet.PlacedAt = DateTime.UtcNow;
        bet.PotentialWinning = request.Amount * request.Odds;
        
         var walletToUpdate = await _walletUpdateOnlyRepository.GetById(wallet.Id);
         walletToUpdate.Balance -= request.Amount;
         
         _walletUpdateOnlyRepository.Update(walletToUpdate);
         
         await _betWriteOnlyRepository.Add(bet);
         
         await _unitOfWork.Commit();

         return _mapper.Map<ResponseBetsJson>(bet);

    }

    public async Task Validate(RequestPlaceBetJson request)
    {
        var validator = new PlaceBetValidator();
        
        var result =  validator.Validate(request);

        if (!result.IsValid)
        {
            var errorMessages = result.Errors
                .Select(error => error.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errorMessages);
        }
        
        
    }
}