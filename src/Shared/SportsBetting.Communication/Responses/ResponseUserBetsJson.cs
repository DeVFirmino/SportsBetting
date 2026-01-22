namespace SportsBetting.Communication.Responses;

public class ResponseUserBetsJson
{
    public List<ResponseBetsJson> Bets { get; set; } = new();
    
    public int TotalCount => Bets?.Count() ?? 0; //Instead of if else
    
}