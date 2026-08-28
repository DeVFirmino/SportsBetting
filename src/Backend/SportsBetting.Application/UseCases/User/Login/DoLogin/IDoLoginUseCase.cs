using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.Login.DoLogin;

public interface IDoLoginUseCase
{
    Task<AuthenticatedUserResponse> Execute(LoginRequest request, CancellationToken cancellationToken);
}
