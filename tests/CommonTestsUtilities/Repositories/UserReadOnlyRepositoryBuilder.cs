using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.User;

namespace SportsBetting.Tests.Common.Repositories;

public class UserReadOnlyRepositoryBuilder
{
    private readonly Mock<IUserReadOnlyRepository> _repository; 
    
    public UserReadOnlyRepositoryBuilder()
    {
        _repository = new Mock<IUserReadOnlyRepository>();
    }

    public void ExistActiveUserWithEmail(string email)
    {
        _repository.Setup(r => r.ExistActiveUserWithEmail(email)).ReturnsAsync(true);
    }
    
    public void GetByEmailAndPassword(User user)
    {
        _repository.Setup(r => r.GetByEmailAndPassword(user.Email, user.Password)).ReturnsAsync(user);
    }
    public IUserReadOnlyRepository Build()
    {
        return _repository.Object;
    }
    

}
