using AutoMapper;
using SportsBetting.Domain.Services.LoggedUser;

namespace SportsBetting.Communication.Responses;

public sealed class GetUserProfileUseCase : IGetUserProfileUseCase
{
    
    private readonly ILoggedUser _loggedUser; 
    private readonly IMapper _mapper;
    
    public GetUserProfileUseCase(ILoggedUser loggedUser,  IMapper mapper)
    {
        _loggedUser = loggedUser;
        _mapper = mapper;
    }
    public async Task<UserProfileResponse> Execute(CancellationToken cancellationToken)
    {
        var user = await _loggedUser.GetUserAsync(cancellationToken);
        
        return _mapper.Map<UserProfileResponse>(user);
    }
}
