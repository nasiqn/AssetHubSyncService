using AssetHub.Shared.Dto;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace AssetHub.Service {
    public class AssetHubSubscriberService : BackgroundService {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSettings _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssetHubSubscriberService> _logger;
        private ServiceBusProcessor? _processor;

        public AssetHubSubscriberService(
            ServiceBusClient client,
            IOptions<ServiceBusSettings> options,
            IServiceScopeFactory scopeFactory,
            ILogger<AssetHubSubscriberService> logger) {
            _client = client;
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken) {
            _processor = _client.CreateProcessor(
                _options.TopicName,
                _options.SubscriptionName,
                new ServiceBusProcessorOptions {
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = _options.MaxConcurrentCalls
                });

            _processor.ProcessMessageAsync += OnMessageAsync;
            _processor.ProcessErrorAsync += OnErrorAsync;

            await _processor.StartProcessingAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        private async Task OnMessageAsync(ProcessMessageEventArgs args) {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<FieldOpsMessageDispatcher>();

            await dispatcher.HandleAsync(args);
        }

        private Task OnErrorAsync(ProcessErrorEventArgs args) {
            _logger.LogError(
                args.Exception,
                "Service Bus error. Entity={EntityPath}, Source={ErrorSource}",
                args.EntityPath,
                args.ErrorSource);

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            if (_processor is not null) {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }

            await _client.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
