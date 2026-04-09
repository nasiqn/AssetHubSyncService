using AssetHub.Shared.Interface;
using AssetHub.Shared.Models;
using AssetHub.Shared.Service;
using AssetHub.Shared.Service.Transformation;
using Azure.Messaging.ServiceBus;

namespace AssetHub.Service {
    public class FieldOpsMessageDispatcher {
        private readonly IAssetSyncCoordinator _coordinator;
        private readonly SyncStatusStore _status;
        private readonly ILogger<FieldOpsMessageDispatcher> _logger;

        public FieldOpsMessageDispatcher(
            IAssetSyncCoordinator coordinator,
            SyncStatusStore status,
            ILogger<FieldOpsMessageDispatcher> logger) {
            _coordinator = coordinator;
            _status = status;
            _logger = logger;
        }

        public async Task HandleAsync(ProcessMessageEventArgs args) {
            var rawJson = args.Message.Body.ToString();

            try {
                await _coordinator.ProcessAsync(rawJson, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                _status.MarkSyncSuccess();
            } catch (PayloadValidationException ex) {
                _logger.LogWarning(ex, "Validation failed for message {MessageId}", args.Message.MessageId);

                await args.DeadLetterMessageAsync(
                    args.Message,
                    deadLetterReason: "ValidationFailed",
                    deadLetterErrorDescription: ex.Message,
                    cancellationToken: args.CancellationToken);

                _status.MarkSyncFailure();
            } catch (Exception ex) {
                _logger.LogError(ex, "Unhandled processing failure for message {MessageId}", args.Message.MessageId);
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                _status.MarkSyncFailure();
            }
        }
    }
}