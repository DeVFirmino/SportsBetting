using Bogus;
using SportsBetting.Communication.Requests;

namespace SportsBetting.Tests.Common.Requests;

public class LoginRequestBuilder
{
    public static LoginRequest Build()
    {
        return new Faker<LoginRequest>()
            .RuleFor(user => user.Email, (f) => f.Internet.Email())
            .RuleFor(user => user.Password, (f) => f.Internet.Password())
            .Generate();
    }
}
