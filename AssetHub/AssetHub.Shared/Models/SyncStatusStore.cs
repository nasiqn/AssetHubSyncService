using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Models {
    public class SyncStatusStore {
        public DateTimeOffset? LastSyncUtc { get; set; }
        public DateTimeOffset? LastDlqReplayUtc { get; set; }
        public int SuccessfulToday { get; private set; }
        public int FailedToday { get; private set; }
        public int ReplayedToday { get; private set; }

        public void MarkSyncSuccess() {
            LastSyncUtc = DateTimeOffset.UtcNow;
            SuccessfulToday++;
        }

        public void MarkSyncFailure() => FailedToday++;
        public void MarkReplaySuccess() => ReplayedToday++;
        public void MarkReplayFailure() => FailedToday++;
    }
}
