using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SportsBetting.Communication.Enums;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace WebApi.Test.Fixture;

/// <summary>
/// With no API-Football key the README flow still runs end to end: log in, deposit, list the
/// offline fixtures and place a bet that debits the wallet and is saved with it.
/// </summary>
public sealed class OfflineFixturesTests : IClassFixture<OfflineFixturesFactory>
{
    private readonly OfflineFixturesFactory _factory;

    public OfflineFixturesTests(OfflineFixturesFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ShouldListTheOfflineFixturesWhenNoApiKeyIsConfigured()
    {
        HttpClient client = await AuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/fixtures");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<FixtureResponse> fixtures = await response.Content.ReadFromJsonAsync<List<FixtureResponse>>() ?? [];
        fixtures.Select(fixture => fixture.FixtureId).Should().Equal(1001, 1002, 1003, 1004, 1005);
        fixtures[0].HomeTeam.Should().Be("Real Madrid");
        fixtures[0].AwayTeam.Should().Be("Barcelona");
    }

    [Fact]
    public async Task ShouldPlaceABetOnAnOfflineFixtureWhenNoApiKeyIsConfigured()
    {
        HttpClient client = await AuthenticatedClient();
        decimal balanceBefore = (await client.GetFromJsonAsync<WalletBalanceResponse>("/wallet"))!.Balance;
        await Deposit(client, 100m);

        HttpResponseMessage response = await PlaceBet(client, fixtureId: 1001, stake: 25m, "offline-bet-1");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        BetResponse? bet = await response.Content.ReadFromJsonAsync<BetResponse>();
        bet.Should().NotBeNull();
        bet!.FixtureId.Should().Be(1001);
        bet.EventName.Should().Be("Real Madrid vs Barcelona");
        bet.Stake.Should().Be(25m);

        WalletBalanceResponse? wallet = await client.GetFromJsonAsync<WalletBalanceResponse>("/wallet");
        wallet!.Balance.Should().Be(balanceBefore + 75m);
        BetResponse? saved = await client.GetFromJsonAsync<BetResponse>($"/bets/{bet.Id}");
        saved!.FixtureId.Should().Be(1001);
    }

    [Fact]
    public async Task ShouldRejectABetWhenTheFixtureIsNotInTheOfflineCatalogue()
    {
        HttpClient client = await AuthenticatedClient();
        await Deposit(client, 50m);

        HttpResponseMessage response = await PlaceBet(client, fixtureId: 123, stake: 10m, "offline-bet-unknown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> AuthenticatedClient()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage tokenResponse = await client.PostAsJsonAsync("/tokens", new LoginRequest
        {
            Email = _factory.GetEmail(),
            Password = _factory.GetPassword(),
        });
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        string? token = document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task Deposit(HttpClient client, decimal amount)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/wallet/deposits",
            new DepositRequest { Amount = amount });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<HttpResponseMessage> PlaceBet(
        HttpClient client,
        int fixtureId,
        decimal stake,
        string idempotencyKey)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/bets")
        {
            Content = JsonContent.Create(new PlaceBetRequest
            {
                FixtureId = fixtureId,
                Stake = stake,
                Market = BettingMarket.HomeWin,
            }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await client.SendAsync(request);
    }
}

public sealed class OfflineFixturesFactory : CustomWebApplicationFactory
{
    protected override bool ReplacesFootballApi => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Settings:FootballApi:ApiKey"] = string.Empty,
            }));
    }
}
