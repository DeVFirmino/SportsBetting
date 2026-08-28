using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.Register;

public interface IRegisterUserUseCase
{
    Task<AuthenticatedUserResponse> Execute(RegisterUserRequest request, CancellationToken cancellationToken);
}
