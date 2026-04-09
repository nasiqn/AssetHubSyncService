using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public sealed record DlqReplayResult(
    int ReadCount,
    int RequeuedCount,
    int FailedCount);
    public interface IDlqReplayService {
        Task<DlqReplayResult> ReplayAsync(CancellationToken cancellationToken = default);
    }
}
