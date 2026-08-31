namespace SportsBetting.Domain.Entities;

public sealed class Wallet : EntityBase
{
    public long UserId { get; set; }
    
    public User User { get; set; } = default!;
    public decimal Balance { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;
    }

    public WalletTransaction Debit(decimal amount, long? betId = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        Balance -= amount;

        return WalletTransaction.BetDebit(UserId, amount, Balance, betId);
    }
}
