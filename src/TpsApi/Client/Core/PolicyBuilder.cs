using TpsApi.Attributes;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Core;

/// <summary>
/// Polly 策略构建器
/// 策略顺序：超时 → 熔断 → 重试
/// </summary>
public static class PolicyBuilder
{
    public static IAsyncPolicy<HttpResponseMessage>? Build(RequestMetadata meta, ILogger logger)
    {
        var policies = new List<IAsyncPolicy<HttpResponseMessage>>();

        if (meta.Timeout != null)
        {
            policies.Add(
                Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMilliseconds(meta.Timeout.TimeoutMs)));
        }

        if (meta.CircuitBreaker != null)
        {
            policies.Add(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        meta.CircuitBreaker.BreakAfterFaults,
                        TimeSpan.FromSeconds(meta.CircuitBreaker.BreakDurationSeconds),
                        onBreak: (outcome, duration) =>
                        {
                            logger.LogError(
                                "[CircuitBreaker] 熔断触发 ServiceCode={ServiceCode} Method={Method} Duration={Duration}s",
                                meta.ServiceCode, meta.MethodName, duration.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            logger.LogInformation(
                                "[CircuitBreaker] 熔断恢复 ServiceCode={ServiceCode} Method={Method}",
                                meta.ServiceCode, meta.MethodName);
                        }));
        }

        if (meta.RetryPolicy != null)
        {
            policies.Add(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        meta.RetryPolicy.RetryCount,
                        retryAttempt => GetWaitDuration(retryAttempt, meta.RetryPolicy),
                        onRetry: (outcome, timeSpan, retryAttempt, _) =>
                        {
                            logger.LogWarning(
                                "[Retry] 第 {RetryAttempt} 次重试 ServiceCode={ServiceCode} Method={Method} Wait={Wait}s",
                                retryAttempt, meta.ServiceCode, meta.MethodName, timeSpan.TotalSeconds);
                        }));
        }

        if (policies.Count == 0) return null;
        if (policies.Count == 1) return policies[0];
        return Policy.WrapAsync([.. policies]);
    }

    private static TimeSpan GetWaitDuration(int retryAttempt, RetryPolicyAttribute policy)
    {
        if (policy.WaitSeconds.Length >= retryAttempt)
            return TimeSpan.FromSeconds(policy.WaitSeconds[retryAttempt - 1]);

        return policy.ExponentialBackoff
            ? TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))
            : TimeSpan.FromSeconds(1);
    }
}
