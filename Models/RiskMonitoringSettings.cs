using System;
using System.Collections.Generic;
using System.Text;

namespace VANewsBot.Models
{
    namespace TelegramBot.Models
    {
        public class RiskMonitoringSettings
        {
            public List<string> RssUrls { get; set; } = new();
            public List<string> TelegramChannels { get; set; } = new();
            public string? IsraeliProxyUrl { get; set; }
            // Poll interval in seconds for the background monitor. Default 5 seconds.
            public int PollIntervalSeconds { get; set; } = 5;
        }
    }
}
