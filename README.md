# SportsBetting API

[![CI](https://github.com/DeVFirmino/SportsBetting/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/DeVFirmino/SportsBetting/actions/workflows/ci.yml)

**Live demo:** [Swagger on Azure Container Apps](https://sportsbetting-api.nicewave-b8afa4cf.westeurope.azurecontainerapps.io/swagger/index.html).
It scales to zero when idle, so the first request takes about 10 seconds. If the
database has paused too, that request can take up to a minute and fail once. See
[Deployment](#deployment).

A study project in .NET: users register, log in, deposit and place bets, with
the balance and the bet saved together.

## What it demonstrates

- REST endpoints with ASP.NET Core controllers
- JWT authentication and ASP.NET Core password hashing
- business rules kept in explicit use cases
- SQL Server persistence with Entity Framework Core
- optimistic concurrency on wallet updates
- idempotent bet placement
- an external football API through `IHttpClientFactory`, with an offline
  fixture catalogue when no key is configured
- unit, HTTP and SQL Server integration tests
- Docker Compose with an explicit migration step

## Main flow

```mermaid
flowchart LR
    A[Register user] --> B[Create token]
    B --> C[Deposit funds]
    C --> D[Choose fixture]
    D --> E[Place bet]
    E --> F[Debit wallet and save bet]
```

When a bet is placed, the API:

1. validates the request and `Idempotency-Key` header;
2. returns the stored result if the same request was already completed;
3. loads the chosen fixture and its server-owned odds;
4. verifies the authenticated user's wallet balance;
5. debits the wallet and saves the bet in one database commit.

The public contract calls the three available markets `HomeWin`, `Draw` and
`AwayWin`. Swagger presents them as text choices, so there is no numeric enum or
undocumented string to guess.

## API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/users` | Register a user and create their wallet |
| `POST` | `/tokens` | Exchange email and password for a JWT |
| `GET` | `/users/me` | Read the authenticated user's profile |
| `PUT` | `/users/me` | Update the profile |
| `PUT` | `/users/me/password` | Change the password |
| `GET` | `/wallet` | Read the current balance |
| `POST` | `/wallet/deposits` | Add study funds to the wallet |
| `GET` | `/fixtures` | List the first ten fixtures of the configured league season |
| `POST` | `/bets` | Place an idempotent bet |
| `GET` | `/bets` | List the authenticated user's bets |
| `GET` | `/bets/{id}` | Read one owned bet |

All routes except registration and token creation require a bearer token.
Registration and token creation are rate-limited per client address.

### Place a bet

Send a unique `Idempotency-Key` header with a maximum of 128 characters:

```http
POST /bets
Authorization: Bearer <token>
Idempotency-Key: portfolio-demo-001
Content-Type: application/json

{
  "fixtureId": 1001,
  "stake": 25.00,
  "market": "HomeWin"
}
```

Fixture `1001` is the first of the offline sample fixtures described under
[Football data](#football-data). With an API-Football key, use an id from
`GET /fixtures` instead.

The fixture name and odds come from the server. The response includes the stake,
odds and potential return. Repeating the same key and payload returns the first
bet without charging the wallet again. Reusing the key with a different payload
returns `409 Conflict`.

## Domain boundaries

The persisted model deliberately has only three concepts:

- `User`: credentials and profile data;
- `Wallet`: the user's current study balance and concurrency token;
- `Bet`: the fixture, market, stake, odds and potential return captured at
  placement time.

There is no transaction ledger, bet settlement engine or administrator flow.
Those concepts would add code without improving the portfolio story this
repository is meant to tell.

The short domain glossary lives in [`CONTEXT.md`](CONTEXT.md).

## Concurrency and idempotency

`Wallet.RowVersion` is an SQL Server `rowversion`. Entity Framework includes it
in the wallet update, so two requests cannot silently spend the same balance.

Bet idempotency is also enforced by a unique database index over user and key.
The use case makes the replay behaviour visible, while the infrastructure layer
translates only the relevant database conflicts. This protects both sequential
retries and requests that arrive at the same time.

## Football data

`GET /fixtures` uses API-Football for fixture and team data. It reads one fixed
league season, La Liga 2024 by default, configured through `Settings:FootballApi`
(`Season` and `LeagueId`), and returns the first ten fixtures the provider lists.
The list is not filtered by date: it is a stable catalogue for exercising the
betting flow, not a live schedule. The integration has a configured timeout and a
short in-memory cache. Upstream failures are exposed
as `502` or `503` Problem Details responses.

Without a key, `GET /fixtures` serves five fixed sample fixtures with ids `1001`
to `1005` instead, so the whole register, deposit and bet flow runs without an
API-Football account. The pairings and kickoff times are illustrative, not the
real schedule. The API logs a warning when it starts serving them. Setting
`Settings:FootballApi:UseOfflineFixtures` to `true` forces the sample even when a
key is set. In the `Production` environment a missing key, or the flag, stops the
application at startup, so a deployment that lost its key fails loudly instead of
offering sample data.

Betting odds are fixed study values owned by this application. `IOddsService`
is the single place that prices a market; its `FixedOddsService` implementation
offers every fixture at the same odds. A client chooses the market, but it
cannot submit or override the odds.

## Project structure

```text
src/
├── Backend/
│   ├── SportsBetting.API             # HTTP endpoints and API configuration
│   ├── SportsBetting.Application     # use cases, validation and mapping
│   ├── SportsBetting.Domain          # entities and repository contracts
│   └── SportsBetting.Infrastructure  # EF Core and football API access
└── Shared/
    ├── SportsBetting.Communication   # request and response contracts
    └── SportsBetting.Exceptions      # application error contract

tests/
├── UseCase.Test                      # isolated business behaviour
├── Validator.Tests                   # request validation
├── WebApi.Test                       # routes, auth, errors and OpenAPI
├── Integration.Test                  # real SQL Server behaviour
└── CommonTestsUtilities              # focused test builders
```

The projects preserve clear dependency boundaries, but the request path remains
ordinary: controller → use case → repository or external service.

## Run with Docker Compose

Prerequisites: Docker Desktop or another Docker-compatible runtime. An
API-Football key is optional.

Create the local environment file:

```bash
cp .env.example .env
```

Edit `.env` and set two values of your own:

- `MSSQL_SA_PASSWORD`: the password for the local SQL Server container. SQL
  Server wants at least eight characters from three of uppercase, lowercase,
  digits and symbols. The repository ships no password, and Compose refuses to
  start while it is empty.
- `JWT_SIGNING_KEY`: a random value of at least 32 characters, for example the
  output of `openssl rand -base64 48`.

Leave `FOOTBALL_API_KEY` empty to use the offline fixtures, or set it to your
API-Football key. Then run:

```bash
docker compose up --build
```

Compose starts SQL Server, runs the versioned Entity Framework migration bundle
and starts the API only after the migration succeeds.

Open Swagger at <http://localhost:8080/swagger>.

The local database port defaults to `1434`, which avoids taking the usual SQL
Server port from an existing installation. Both published ports can be changed
in `.env`.

### Try the flow in Swagger

1. `POST /users` with a name, an email and a password of at least six
   characters.
2. `POST /tokens` with the same email and password, and copy
   `tokens.accessToken` from the response.
3. Select **Authorize** and paste the token.
4. `POST /wallet/deposits` with `{ "amount": 100 }`.
5. `GET /fixtures` to see the fixtures and their odds.
6. `POST /bets` with an `Idempotency-Key` header and the body from
   [Place a bet](#place-a-bet).
7. `GET /wallet` shows the balance after the stake, and `GET /bets` lists the bet.

### Run from an IDE or with `dotnet run`

The launch profile carries only settings that are not secret. The connection
string and the signing key come from [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets),
which stay outside the repository. With `.env` filled in as above, start SQL
Server and apply the migrations with Compose, then run the API from source:

```bash
docker compose run --rm migrator

dotnet user-secrets --project src/Backend/SportsBetting.API set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1434;Database=SportsBetting;User Id=sa;Password=<your MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
dotnet user-secrets --project src/Backend/SportsBetting.API set \
  "Settings:Jwt:SigningKey" "<at least 32 random characters>"

dotnet run --project src/Backend/SportsBetting.API
```

Swagger is then at <http://localhost:5055/swagger>. To call API-Football instead
of the offline fixtures, also set `Settings:FootballApi:ApiKey` as a user secret.

## Deployment

The demo is online since 23 September 2026:
[Swagger](https://sportsbetting-api.nicewave-b8afa4cf.westeurope.azurecontainerapps.io/swagger/index.html).
It runs on Azure Container Apps in West Europe, with Azure SQL Database behind it.

Two things make it slow after a quiet spell:

- the Container App scales to zero, so the first request after idle takes about
  10 seconds;
- the database is Azure SQL serverless on the free offer and pauses after 60
  minutes idle, so the first request after a pause can take up to a minute and may
  fail once while the database resumes.

The demo does not call API-Football. It runs with `ASPNETCORE_ENVIRONMENT=Demo`
and serves the offline fixture catalogue, fixtures `1001` to `1005`, because
`Production` requires an API-Football key and none is configured.

It was deployed by hand with the `az` CLI, into the resource group
`sportsbetting-rg`:

1. build the runtime and migrator targets of the Dockerfile in Azure Container
   Registry (`danielfirmino.azurecr.io`) with `az acr build`;
2. run a Container Apps job that applies the Entity Framework migration bundle to
   Azure SQL, with the connection string in `SPORTSBETTING_EF_CONNECTION`;
3. create the Container App with `az containerapp create`, with the connection
   string and the JWT signing key stored as Container App secrets.

The running image is `sportsbetting-api:v16`, built from `develop` at `23192b1`.
On 23 September 2026 the flow was checked over HTTPS: register returned `201`,
login issued a token, a deposit returned `204`, `GET /fixtures` listed the five
offline fixtures, `POST /bets` returned `201` for a stake of 25 with a potential
return of 52.5, replaying it with the same `Idempotency-Key` returned the same
bet id, and the wallet balance was 75.

Configuration comes from environment variables on the Container App, the same
names Compose uses locally. CI builds and tests; it does not deploy.
[How I containerised my API and deployed it to Azure](https://danieldias.dev/en/blog/how-i-containerised-my-api-and-deployed-it-to-azure)
walks through the Dockerfile and the Container Apps setup.

## Build and test

The repository targets the .NET 10 SDK:

```bash
dotnet build SportsBetting.sln
dotnet test SportsBetting.sln --no-build
```

The five integration tests require Docker because they start a disposable SQL
Server with Testcontainers. They cover the database behaviours that matter most:

- registration persists the user and wallet together;
- placement debits the wallet and saves the bet together;
- replaying an idempotency key does not debit twice;
- concurrent requests with the same key create one bet;
- concurrent requests with different keys cannot overdraw the wallet.

The placement and concurrency cases are in
[`PlaceBetUseCaseTests`](tests/Integration.Test/PlaceBetUseCaseTests.cs).

Run only that suite with:

```bash
dotnet test tests/Integration.Test/Integration.Test.csproj
```

## Technology

- .NET 10 and C#
- ASP.NET Core Web API
- Entity Framework Core and SQL Server
- AutoMapper and FluentValidation
- JWT bearer authentication
- Swagger / OpenAPI
- xUnit, FluentAssertions and Testcontainers
- Docker and Docker Compose

## Intentional trade-offs

This API is sized for learning and portfolio review, not production betting.
Deposits create study funds, odds are static, fixtures are read-only and bets do
not transition after placement. The code still protects the two behaviours
where correctness is worth demonstrating: wallet concurrency and safe request
retries.
