using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramBot.Services
{
    public class OrefAlertService
    {
        private readonly HttpClient _client;

        public OrefAlertService(string? israeliProxyUrl = null)
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(israeliProxyUrl))
            {
                handler.Proxy = new WebProxy(israeliProxyUrl);
                handler.UseProxy = true;
            }

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            _client.DefaultRequestHeaders.Add("Referer", "https://www.oref.org.il/");
            _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; NewsRiskBot/1.0)");
        }

        public async Task<List<string>> GetActiveAlertCitiesAsync()
        {
            try
            {
                var json = await _client.GetStringAsync("https://www.oref.org.il/WarningMessages/alert/alerts.json");

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<string>();
                }

                json = json.Trim().TrimStart('\uFEFF');

                if (json == "[]" || json == "\"\"" || json == "null")
                {
                    return new List<string>();
                }

                var alert = JsonSerializer.Deserialize<OrefAlert>(json);
                return alert?.Data ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки тревоги Пикуд ха-Ореф: {ex.Message}");
                return new List<string>();
            }
        }

        private class OrefAlert
        {
            [JsonPropertyName("data")]
            public List<string>? Data { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("category")]
            public int Category { get; set; }
        }
    }
}