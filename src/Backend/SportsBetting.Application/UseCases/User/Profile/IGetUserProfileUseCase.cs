namespace SportsBetting.Communication.Responses;

public interface IGetUserProfileUseCase
{
    public Task<ResponseUserProfileJson> Execute();
}