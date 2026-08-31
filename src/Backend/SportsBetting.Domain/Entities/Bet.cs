using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

public sealed class Bet : EntityBase
{
    public long UserId { get; set; }

    public User User { get; set; } = default!;
    
    public int FixtureId { get; set; }
    
    public decimal Amount { get; set; }
    public decimal Odds { get; set; }
    public decimal PotentialWinning { get; set; }
    public string EventName { get; set; } = string.Empty;
    public BetType BetType { get; set; }  
    public BetStatus Status { get; set; }
    
    public DateTime PlacedAt { get; set; }
    
    
    public DateTime? SettledAt { get; set; }

    public string? ClientRequestId { get; set; }

    public static Bet Place(
        long userId,
        int fixtureId,
        decimal amount,
        BetType betType,
        decimal odds,
        string eventName,
        string? clientRequestId,
        DateTime placedAt)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (odds <= 0)
            throw new ArgumentOutOfRangeException(nameof(odds));

        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        return new Bet
        {
            UserId = userId,
            FixtureId = fixtureId,
            Amount = amount,
            BetType = betType,
            Odds = odds,
            PotentialWinning = amount * odds,
            EventName = eventName,
            Status = BetStatus.Pending,
            PlacedAt = placedAt,
            ClientRequestId = string.IsNullOrWhiteSpace(clientRequestId) ? null : clientRequestId.Trim(),
        };
    }
 
}
