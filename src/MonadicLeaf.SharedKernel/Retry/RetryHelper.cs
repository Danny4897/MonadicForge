using MonadicSharp;

namespace MonadicLeaf.SharedKernel.Retry;

/// <summary>
/// Green-code: WithRetry outside validation scope — never retry validation errors.
/// Always uses jitter to avoid thundering herd on LLM rate limits.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Retries the operation with exponential backoff + jitter.
    /// Only retries when the error code is retriable (rate limit, timeout, unavailable).
    /// </summary>
    public static async Task<Result<T>> WithRetry<T>(
        Func<Task<Result<T>>> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        bool useJitter = true)
    {
        var baseDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        Result<T> last = Result<T>.Failure(Error.Create("Retry never attempted", "RETRY_INIT"));

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            last = await operation();

            if (last.IsSuccess) return last;
            if (!IsRetriable(last.Error)) return last; // Fail fast on non-retriable errors
            if (attempt < maxAttempts - 1)
                await Task.Delay(ComputeDelay(baseDelay, attempt, useJitter));
        }

        return last;
    }

    private static TimeSpan ComputeDelay(TimeSpan baseDelay, int attempt, bool useJitter)
    {
        var exponential = baseDelay * Math.Pow(2, attempt);
        if (!useJitter) return exponential;
        // Jitter: 50-100% of exponential delay
        return exponential * (0.5 + Random.Shared.NextDouble() * 0.5);
    }

    private static bool IsRetriable(Error error) =>
        error.Code is "RATE_LIMIT" or "MODEL_TIMEOUT" or "MODEL_UNAVAILABLE"
            or "LEAF_LLM_RATE_LIMIT" or "LEAF_LLM_TIMEOUT";
}
