using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.BetRepository;

namespace SportsBetting.Tests.Common.Repositories;

public sealed class BetReadOnlyRepositoryBuilder
{
    private readonly Mock<IBetReadOnlyRepository> _repository = new();

    public BetReadOnlyRepositoryBuilder GetPagedByUserIdAsync(List<Bet> bets, int totalCount)
    {
        _repository.Setup(r => r.GetPagedByUserIdAsync(
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((bets, totalCount));

        return this;
    }

    public IBetReadOnlyRepository Build() => _repository.Object;
}
