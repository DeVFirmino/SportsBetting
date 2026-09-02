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

    public void Debit(decimal stake)
    {
        if (stake <= 0)
            throw new ArgumentOutOfRangeException(nameof(stake));

        if (Balance < stake)
            throw new InvalidOperationException("Insufficient balance.");

        Balance -= stake;
    }
}
