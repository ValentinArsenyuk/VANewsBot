using System;
using System.Collections.Generic;
using System.Text;

namespace VANewsBot.Models
{
    public class TelegramSettings
    {
        public string Token { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
    }
}
