namespace SportsBetting.Communication.Requests;

public sealed class UpdateUserRequest
{

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
