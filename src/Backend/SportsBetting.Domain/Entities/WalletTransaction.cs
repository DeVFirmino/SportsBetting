using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// One append-only entry in a wallet's ledger. Entries are created by <see cref="Wallet"/>, which
/// is the only thing that knows the balance they record.
/// </summary>
public sealed class WalletTransaction : EntityBase
{
    public long WalletId { get; set; }

    public Wallet Wallet { get; set; } = default!;

    public long? BetId { get; set; }

    public Bet? Bet { get; set; }

    public WalletTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? ClientRequestId { get; set; }
}
