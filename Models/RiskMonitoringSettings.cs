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
        }
    }
}
