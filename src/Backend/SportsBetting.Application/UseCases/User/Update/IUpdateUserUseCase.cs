using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.User.Update;

public interface IUpdateUserUseCase
{
    public Task Execute(RequestUpdateUserJson request);

}