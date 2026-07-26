namespace TelegramBot.Models
{
    public class RiskResult
    {
        public int Score { get; set; }

        public List<string> Reasons { get; set; } = new();

        public List<string> NewsTitles { get; set; } = new();
    }
}