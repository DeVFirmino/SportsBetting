namespace SportsBetting.Domain.Security.Tokens;

public interface IAccessTokenGenerator
{
    string Generate(Guid userIdentifier);

}