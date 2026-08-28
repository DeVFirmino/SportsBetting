namespace SportsBetting.Communication.Requests;

public sealed class ChangePasswordRequest
{
    public string Password { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

}
