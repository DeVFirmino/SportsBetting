using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.User.Update;

public interface IUpdateUserUseCase
{
    Task Execute(UpdateUserRequest request, CancellationToken cancellationToken);

}
