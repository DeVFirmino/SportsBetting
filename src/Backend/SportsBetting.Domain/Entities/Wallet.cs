using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

public sealed class Wallet : EntityBase
{
    public long UserId { get; set; }

    public User User { get; set; } = default!;

    public decimal Balance { get; set; }

    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Credits the wallet and returns the ledger entry that records the move. The entry points at
    /// this wallet through the navigation property, so a wallet created in the same commit still
    /// gets a correct foreign key.
    /// </summary>
    public WalletTransaction Deposit(decimal amount, string? clientRequestId)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;

        return new WalletTransaction
        {
            Wallet = this,
            Type = WalletTransactionType.Deposit,
            Amount = amount,
            BalanceAfter = Balance,
            OccurredAt = DateTime.UtcNow,
            ClientRequestId = clientRequestId,
        };
    }

    public WalletTransaction Debit(decimal amount, Bet bet)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        Balance -= amount;

        return new WalletTransaction
        {
            Wallet = this,
            Bet = bet,
            Type = WalletTransactionType.BetDebit,
            Amount = amount,
            BalanceAfter = Balance,
            OccurredAt = DateTime.UtcNow,
            ClientRequestId = bet.ClientRequestId,
        };
    }
}
