using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Enums;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace WebApi.Test.Bet;

public sealed class BetEndpointTests
{
    [Fact]
    public async Task ShouldReturnCreatedWithLocationWhenBetIsPlaced()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);
        await Deposit(client, 100m);

        HttpResponseMessage response = await PlaceBet(client, stake: 20m, "key-1");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        BetResponse? bet = await response.Content.ReadFromJsonAsync<BetResponse>();
        bet.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/bets/{bet!.Id}");
        bet.Market.Should().Be("HomeWin");
    }

    [Fact]
    public async Task ShouldReturnOkWithEmptyCollectionWhenUserHasNoBets()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/bets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<BetResponse>? page = await response.Content
            .ReadFromJsonAsync<PagedResponse<BetResponse>>();
        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenBetDoesNotExist()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/bets/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenBetRequestIsInvalid()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        HttpResponseMessage response = await PlaceBet(client, stake: 0m, "key-1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenIdempotencyKeyIsMissing()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync("/bets", ValidRequest(20m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnConflictWhenIdempotencyKeyIsReusedWithDifferentPayload()
    {
        await using CustomWebApplicationFactory factory = new();
        HttpClient client = await AuthenticatedClient(factory);
        await Deposit(client, 100m);
        (await PlaceBet(client, stake: 20m, "key-1")).StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage conflict = await PlaceBet(client, stake: 30m, "key-1");

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        WalletBalanceResponse? wallet = await client.GetFromJsonAsync<WalletBalanceResponse>("/wallet");
        wallet!.Balance.Should().Be(80m);
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

    private static async Task Deposit(HttpClient client, decimal amount)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/wallet/deposits",
            new DepositRequest { Amount = amount });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<HttpResponseMessage> PlaceBet(
        HttpClient client,
        decimal stake,
        string idempotencyKey)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/bets")
        {
            Content = JsonContent.Create(ValidRequest(stake)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await client.SendAsync(request);
    }

    private static PlaceBetRequest ValidRequest(decimal stake) => new()
    {
        FixtureId = 123,
        Stake = stake,
        Market = BettingMarket.HomeWin,
    };
}
