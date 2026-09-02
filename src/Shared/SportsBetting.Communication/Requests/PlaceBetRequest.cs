using SportsBetting.Communication.Enums;

namespace SportsBetting.Communication.Requests;

public sealed class PlaceBetRequest
{
    public int FixtureId { get; set; }

    public decimal Stake { get; set; }

    public BettingMarket? Market { get; set; }
}
