using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Dto {
    public class DlqSettings {
        public const string SectionName = "Dlq";
        public string QueueName { get; set; } = string.Empty;
        public int MaxMessagesPerRun { get; set; } = 100;
    }
}
