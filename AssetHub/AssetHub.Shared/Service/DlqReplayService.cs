using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using AssetHub.Shared.Models;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service {
    public class DlqReplayService: IDlqReplayService {
        private readonly ServiceBusClient _client;
        private readonly DlqSettings _options;
        private readonly ILogger<DlqReplayService> _logger;
        private readonly SyncStatusStore _status;

        public DlqReplayService(
            ServiceBusClient client,
            IOptions<DlqSettings> options,
            ILogger<DlqReplayService> logger,
            SyncStatusStore status) {
            _client = client;
            _options = options.Value;
            _logger = logger;
            _status = status;
        }

        public async Task<DlqReplayResult> ReplayAsync(CancellationToken cancellationToken = default) {
            int read = 0, requeued = 0, failed = 0;

            await using var receiver = _client.CreateReceiver(
                _options.QueueName,
                new ServiceBusReceiverOptions {
                    SubQueue = SubQueue.DeadLetter,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

            await using var sender = _client.CreateSender(_options.QueueName);

            IReadOnlyList<ServiceBusReceivedMessage> messages =
                await receiver.ReceiveMessagesAsync(_options.MaxMessagesPerRun, TimeSpan.FromSeconds(5), cancellationToken);

            foreach (var message in messages) {
                read++;

                try {
                    var replay = new ServiceBusMessage(message.Body) {
                        Subject = message.Subject,
                        ContentType = message.ContentType,
                        CorrelationId = message.CorrelationId,
                        MessageId = message.MessageId, // preserve identity for tracing/idempotency
                        ApplicationProperties =
                        {
                        ["replayedFromDlq"] = true,
                        ["replayedAtUtc"] = DateTimeOffset.UtcNow.ToString("O")
                    }
                    };

                    foreach (var kvp in message.ApplicationProperties) {
                        if (!replay.ApplicationProperties.ContainsKey(kvp.Key)) {
                            replay.ApplicationProperties[kvp.Key] = kvp.Value;
                        }
                    }

                    await sender.SendMessageAsync(replay, cancellationToken);
                    await receiver.CompleteMessageAsync(message, cancellationToken);

                    requeued++;
                    _status.MarkReplaySuccess();
                } catch (Exception ex) {
                    failed++;
                    _status.MarkReplayFailure();
                    _logger.LogError(ex, "Failed to replay DLQ message {MessageId}.", message.MessageId);

                    // Let lock expire or abandon so it stays in DLQ for another run.
                    await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                }
            }

            _status.LastDlqReplayUtc = DateTimeOffset.UtcNow;
            return new DlqReplayResult(read, requeued, failed);
        }
    }
}
