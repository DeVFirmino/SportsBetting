using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

/// <summary>
/// The resilience pipeline in front of API-Football.
/// </summary>
public static class FootballApiResilience
{
    public static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    public const int MaxRetryAttempts = 3;

    public static void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        FootballApiOptions options)
    {
        builder.AddTimeout(TotalTimeout);

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds),
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

        // The per-attempt timeout sits inside this retry, so a slow upstream arrives here as
        // TimeoutRejectedException. Leaving it out made the most common transient failure the one
        // failure the pipeline never retried.
        if (args.Outcome.Exception is HttpRequestException or TaskCanceledException or TimeoutRejectedException)
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
