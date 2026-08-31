using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

/// <summary>
/// The resilience pipeline in front of API-Football. It lives here rather than inline in the
/// registration so the exact policy the application runs is the one the tests drive.
/// </summary>
public static class FootballApiResilience
{
    public static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    public const int MaxRetryAttempts = 3;

    public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder)
        => Configure(builder, RetryDelay);

    /// <param name="retryDelay">
    /// The base backoff delay. Overridable so tests can exercise the same policy without waiting
    /// out the production delays.
    /// </param>
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder, TimeSpan retryDelay)
    {
        builder.AddTimeout(TotalTimeout);

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = retryDelay,
            BackoffType = DelayBackoffType.Exponential,
            // Jitter keeps a fleet of callers from retrying in lockstep after a shared outage.
            UseJitter = true,
            ShouldHandle = static args => ValueTask.FromResult(ShouldRetry(args)),
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(30),
        });

        builder.AddTimeout(AttemptTimeout);
    }

    private static bool ShouldRetry(RetryPredicateArguments<HttpResponseMessage> args)
    {
        // Only GET is replayed. Retrying anything that could change state upstream would risk
        // performing the same write twice, so a non-GET failure is surfaced instead.
        if (IsSafeToReplay(args) is false)
            return false;

        if (args.Outcome.Exception is HttpRequestException or TaskCanceledException)
            return true;

        return args.Outcome.Result?.StatusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static bool IsSafeToReplay(RetryPredicateArguments<HttpResponseMessage> args)
    {
        HttpRequestMessage? request = args.Outcome.Result?.RequestMessage
            ?? args.Context.GetRequestMessage();

        return request is null || request.Method == HttpMethod.Get;
    }
}
