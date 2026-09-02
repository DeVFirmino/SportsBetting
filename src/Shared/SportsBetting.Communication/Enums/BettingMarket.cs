using System.Text.Json.Serialization;

namespace SportsBetting.Communication.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BettingMarket
{
    HomeWin,
    Draw,
    AwayWin,
}
