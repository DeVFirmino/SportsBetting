namespace SportsBetting.Communication.Responses;

public sealed class AuthenticatedUserResponse
{
    public string Name { get; set; } = string.Empty;

    public AccessTokenResponse Tokens { get; set; } = new();
}
