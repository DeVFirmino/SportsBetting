namespace SportsBetting.Communication.Responses;

public interface IGetUserProfileUseCase
{
    Task<UserProfileResponse> Execute(CancellationToken cancellationToken);
}
