using System.Net;
using System.Text.RegularExpressions;

namespace TelegramBot.Services
{
    public class TelegramChannelService
    {
        private readonly List<string> _channels;

        private static readonly Regex PostTextRegex = new(
            @"<div class=""tgme_widget_message_text[^""]*""[^>]*>(.*?)</div>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlTagRegex = new(
            @"<[^>]+>", RegexOptions.Compiled);

        public TelegramChannelService(List<string> channels)
        {
            _channels = channels;
        }

        public async Task<List<string>> GetLatestPostsAsync(HttpClient client)
        {
            var allPosts = new List<string>();

            foreach (var channel in _channels)
            {
                try
                {
                    var html = await client.GetStringAsync($"https://t.me/s/{channel}");
                    var matches = PostTextRegex.Matches(html);

                    foreach (Match match in matches)
                    {
                        var rawText = match.Groups[1].Value;
                        rawText = Regex.Replace(rawText, @"<br\s*/?>", " ");

                        var text = HtmlTagRegex.Replace(rawText, "");
                        text = WebUtility.HtmlDecode(text).Trim();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            allPosts.Add(text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки Telegram-канала ({channel}): {ex.Message}");
                }
            }

            return allPosts;
        }
    }
}