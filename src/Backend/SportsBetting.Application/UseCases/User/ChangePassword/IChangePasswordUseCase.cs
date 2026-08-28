using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.User.ChangePassword;

public interface IChangePasswordUseCase
{
    Task Execute(ChangePasswordRequest request, CancellationToken cancellationToken);
}
