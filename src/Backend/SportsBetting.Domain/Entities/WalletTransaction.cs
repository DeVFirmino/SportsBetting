using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

public sealed class WalletTransaction : EntityBase
{
    public long UserId { get; set; }

    public User User { get; set; } = default!;

    public long? BetId { get; set; }

    public Bet? Bet { get; set; }

    public WalletTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public DateTime OccurredAt { get; set; }

    public static WalletTransaction Deposit(long userId, decimal amount, decimal balanceAfter)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        return new WalletTransaction
        {
            UserId = userId,
            Type = WalletTransactionType.Deposit,
            Amount = amount,
            BalanceAfter = balanceAfter,
            OccurredAt = DateTime.UtcNow,
        };
    }

    public static WalletTransaction BetDebit(long userId, decimal amount, decimal balanceAfter, long? betId)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        return new WalletTransaction
        {
            UserId = userId,
            BetId = betId,
            Type = WalletTransactionType.BetDebit,
            Amount = amount,
            BalanceAfter = balanceAfter,
            OccurredAt = DateTime.UtcNow,
        };
    }
}
