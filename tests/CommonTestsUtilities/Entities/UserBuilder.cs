using Bogus;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Tests.Common.Cryptography;

namespace SportsBetting.Tests.Common.Entities;

public class UserBuilder
{
    public static (User user, string password) Build()
    {
        IPasswordHasher passwordHasher = PasswordHasherBuilder.Build();

        string password = new Faker().Internet.Password();

        User user = new Faker<User>()
            .RuleFor(user => user.Id, () => 1)
            .RuleFor(user => user.Name, faker => faker.Person.FirstName)
            .RuleFor(user => user.Email, (faker, user) => faker.Internet.Email(user.Name))
            .RuleFor(user => user.UserIdentifier, () => Guid.NewGuid());

        // Seeded the way the application stores it: through the same hasher login verifies with.
        user.Password = passwordHasher.Hash(user, password);

        return (user, password);
    }
}
