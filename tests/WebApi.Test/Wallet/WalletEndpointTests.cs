using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace WebApi.Test.Wallet;

public sealed class WalletEndpointTests
{
    [Fact]
    public async Task ShouldReturnNoContentWhenDepositIsValid()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        HttpResponseMessage response = await Deposit(client, 25m);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ShouldApplyEveryDepositWhenSameRequestIsSubmittedTwice()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        (await Deposit(client, 25m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Deposit(client, 25m)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        WalletBalanceResponse? wallet = await client.GetFromJsonAsync<WalletBalanceResponse>("/wallet");
        wallet!.Balance.Should().Be(50m);
    }

    private static Task<HttpResponseMessage> Deposit(HttpClient client, decimal amount)
    {
        return client.PostAsJsonAsync("/wallet/deposits", new DepositRequest { Amount = amount });
    }

    private static async Task<HttpClient> AuthenticatedClient(CustomWebApplicationFactory factory)
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage tokenResponse = await client.PostAsJsonAsync("/tokens", new LoginRequest
        {
            Email = factory.GetEmail(),
            Password = factory.GetPassword(),
        });
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        string? token = document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
