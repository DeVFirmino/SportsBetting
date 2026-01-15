using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

public class Bet : EntityBase
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
 
}