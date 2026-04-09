using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using AssetHub.Shared.Models;
using AssetHub.Shared.Service;
using AssetHub.Shared.Service.Transformation;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System.Net;

namespace AssetHub.Shared {
    public static class AssetHubServiceExtensions {
        public static IServiceCollection AddAssetHubClient(this IServiceCollection services, IConfiguration configuration) {
            services
                .AddOptions<AssetHubSettings>()
                .Bind(configuration.GetSection(AssetHubSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(
                    o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _)
                      && Uri.TryCreate(o.TokenUrl, UriKind.Absolute, out _),
                    "AssetHub URLs must be absolute.")
                .ValidateOnStart();

            services
                .AddOptions<ResilienceSettings>()
                .Bind(configuration.GetSection(ResilienceSettings.SectionName));
            services
                .AddOptions<DlqSettings>()
                .Bind(configuration.GetSection(DlqSettings.SectionName));


            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IAssetHubCircuitState, AssetHubCircuitState>();
            services.AddSingleton<SyncStatusStore>();

            services.AddHttpClient<IAssetHubTokenProvider, AssetHubTokenProvider>();

            services.AddTransient<AssetHubRequestHandler>();
            services.AddTransient<AssetHubRequestRetryHandler>();
            services.AddTransient<IFieldOpsAssetPayloadTransformer, FieldOpsAssetPayloadTransformer>();
            services.AddTransient<IAssetSyncCoordinator, AssetSyncCoordinator>();

            services
                .AddHttpClient<IAssetHubClient, AssetHubClient>((sp, httpClient) => {
                    var options = sp.GetRequiredService<IOptions<AssetHubSettings>>().Value;
                    httpClient.BaseAddress = new Uri(options.BaseUrl);
                })
                .AddHttpMessageHandler<AssetHubRequestRetryHandler>()
                .AddHttpMessageHandler<AssetHubRequestHandler>()
                .AddResilienceHandler("assethub-pipeline", (builder, context) => {
                    var settings = context.ServiceProvider.GetRequiredService<IOptions<ResilienceSettings>>().Value;
                    var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AssetHubResilience");
                    var circuit = context.ServiceProvider.GetRequiredService<IAssetHubCircuitState>();

                    builder.AddRetry(new HttpRetryStrategyOptions {
                        MaxRetryAttempts = settings.Retry.MaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = settings.Retry.UseJitter,
                        Delay = TimeSpan.FromMilliseconds(settings.Retry.BaseDelayMs),
                        ShouldHandle = args => ValueTask.FromResult(
                            args.Outcome.Exception is TimeoutRejectedException ||
                            args.Outcome.Result is HttpResponseMessage r &&
                            (r.StatusCode == HttpStatusCode.RequestTimeout ||
                                r.StatusCode == HttpStatusCode.TooManyRequests ||
                                (int)r.StatusCode >= 500)),
                        OnRetry = args => {
                            logger.LogWarning(
                                "Retrying AssetHub call. Attempt {Attempt}, delay {DelayMs} ms.",
                                args.AttemptNumber,
                                args.RetryDelay.TotalMilliseconds);
                            return default;
                        }
                    });

                    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions {
                        FailureRatio = settings.CircuitBreaker.FailureRatio,
                        MinimumThroughput = settings.CircuitBreaker.MinimumThroughput,
                        SamplingDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.SamplingDurationSeconds),
                        BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.BreakDurationSeconds),
                        ShouldHandle = args => ValueTask.FromResult(
                            args.Outcome.Exception is TimeoutRejectedException ||
                            args.Outcome.Result is HttpResponseMessage r &&
                            (r.StatusCode == HttpStatusCode.RequestTimeout ||
                                r.StatusCode == HttpStatusCode.TooManyRequests ||
                                (int)r.StatusCode >= 500)),
                        OnOpened = args => {
                            circuit.MarkOpened();
                            logger.LogError(
                                "AssetHub circuit opened for {BreakSeconds} seconds.",
                                settings.CircuitBreaker.BreakDurationSeconds);
                            return default;
                        },
                        OnClosed = args
                            => new ValueTask(Task.Run(() => {
                                circuit.MarkClosed();
                                logger.LogInformation("AssetHub circuit closed.");
                            })),
                        OnHalfOpened = args
                            => new ValueTask(Task.Run(() => {
                                circuit.MarkHalfOpen();
                                logger.LogInformation("AssetHub circuit half-open.");
                            }))
                    });
                    builder.AddTimeout(TimeSpan.FromSeconds(settings.Timeout.AttemptTimeoutSeconds));
                });

            services.AddSingleton<IDlqReplayService, DlqReplayService>();
            services.AddSingleton<SyncStatusStore>();

            return services;
        }
    }
}
