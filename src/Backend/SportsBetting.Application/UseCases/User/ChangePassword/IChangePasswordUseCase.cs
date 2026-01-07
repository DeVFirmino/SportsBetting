using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.User.ChangePassword;

public interface IChangePasswordUseCase
{
    public Task Execute(RequestChangePasswordJson request);
}