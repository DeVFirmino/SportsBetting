using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.Profile;

public interface IGetUserProfileUseCase
{
    Task<UserProfileResponse> Execute(CancellationToken cancellationToken);
}
