using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.Register;

public interface IRegisterUserUseCase
{
    public Task <ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request);
}