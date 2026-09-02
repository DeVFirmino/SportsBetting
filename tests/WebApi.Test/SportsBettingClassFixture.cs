using System.Net.Http.Json;
using SportsBetting.Communication.Requests;

namespace WebApi.Test;

public class SportsBettingClassFixture : IClassFixture<CustomWebApplicationFactory>
{

    private readonly HttpClient _httpClient;

    public SportsBettingClassFixture(CustomWebApplicationFactory factory) => _httpClient = factory.CreateClient();

    protected async Task<HttpResponseMessage> DoPost(string method, object request, string culture = "en-US")
    {
        ChangeRequestCulture(culture);

        return await _httpClient.PostAsJsonAsync(method, request);
    }

    private void ChangeRequestCulture(string culture)
    {
        if (_httpClient.DefaultRequestHeaders.Contains("Accept-Language"))
            _httpClient.DefaultRequestHeaders.Remove("Accept-Language");

        _httpClient.DefaultRequestHeaders.Add("Accept-Language", culture);
    }
}
