namespace SportsBetting.Infrastructure.ExternalServices.DTOs;

public class ApiFootballResponse<T>
{
    public List<T> Response { get; set; } = new();
}