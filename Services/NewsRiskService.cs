using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using TelegramBot.Models;

namespace TelegramBot.Services
{
    public class NewsRiskService
    {
        // kept for compatibility with hot-reload / incremental builds
        private readonly string rssUrl = "https://www.timesofisrael.com/feed/";

        private readonly List<string> _rssUrls;
        private readonly OrefAlertService _orefAlertService;
        private readonly TelegramChannelService _telegramChannelService;

        public NewsRiskService(
            List<string> rssUrls,
            OrefAlertService orefAlertService,
            TelegramChannelService telegramChannelService)
        {
            _rssUrls = rssUrls;
            _orefAlertService = orefAlertService;
            _telegramChannelService = telegramChannelService;
        }

        private static readonly XmlReaderSettings SafeXmlSettings = new()
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024
        };

        private static readonly Regex RssLinkRegex = new(
            @"<link[^>]+rel=[""']alternate[""'][^>]+type=[""']application/(rss|atom)\+xml[""'][^>]+href=[""']([^""']+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<RiskResult> CalculateRisk()
        {
            var result = new RiskResult();

            var activeCities = await _orefAlertService.GetActiveAlertCitiesAsync();

            if (activeCities.Count > 0)
            {
                result.Score = 100;
                result.Reasons.Add("🚨 АКТИВНАЯ ТРЕВОГА: " + string.Join(", ", activeCities));
                result.NewsTitles.Add("Активная тревога Пикуд ха-Ореф: " + string.Join(", ", activeCities));

                Console.WriteLine($"АКТИВНАЯ ТРЕВОГА в городах: {string.Join(", ", activeCities)}");
                return result;
            }

            var keywords = new Dictionary<string, (int Score, string Reason)>
            {
                // English
                { "missile launch", (40, "🚀 Запуск ракет") },
                { "missile", (25, "🚀 Ракетная угроза") },
                { "rocket", (25, "🚀 Сообщения о ракетах") },
                { "airstrike", (35, "✈️ Авиационный удар") },
                { "strike", (20, "💥 Военный удар") },
                { "attack", (15, "⚠️ Сообщение об атаке") },
                { "retaliation", (20, "⚔️ Угроза ответного удара") },
                { "nuclear", (15, "☢️ Ядерная тема") },
                { "irgc", (10, "🎖 Активность КСИР") },
                { "war", (15, "⚔️ Упоминание войны") },

                // Hebrew
                { "טיל", (25, "🚀 טיל / ракета") },
                { "טילים", (25, "🚀 טילים / ракеты") },
                { "רקטה", (25, "🚀 רקטה / ракета") },
                { "תקיפה", (30, "✈️ תקיפה / удар") },
                { "אזעקה", (35, "🚨 אזעקה / сирена") },
                { "פיצוץ", (25, "💥 פיצוץ / взрыв") },
                { "מלחמה", (15, "⚔️ מלחמה / война") },
                { "כטב\"ם", (20, "🛩 כטב\"ם / БПЛА") },

                // Russian
                { "ракета", (25, "🚀 Упоминание ракеты") },
                { "ракеты", (25, "🚀 Упоминание ракет") },
                { "обстрел", (30, "💥 Сообщение об обстреле") },
                { "обстрелы", (30, "💥 Сообщение об обстрелах") },
                { "удар", (25, "💥 Военный удар") },
                { "атака", (20, "⚠️ Сообщение об атаке") },
                { "бомба", (25, "💣 Упоминание бомбы") },
                { "взрыв", (25, "💥 Взрыв") },
                { "сирена", (35, "🚨 Сирена / тревога") },
                { "тревога", (30, "🚨 Тревога / сирена") },
                { "война", (15, "⚔️ Упоминание войны") },
                { "боевики", (20, "⚔️ Боевики / террористы") },
                { "террор", (25, "⚠️ Террор / теракт") },
                { "террорист", (25, "⚠️ Террорист") },
                { "сбит", (20, "✈️ Сбитый / ПВО") },
                { "уничтожен", (20, "💥 Уничтожение цели") },
                { "погиб", (20, "⚰️ Погибший / жертва") },
                { "погибли", (20, "⚰️ Погибшие / жертвы") },
                { "ранен", (15, "🚑 Раненый / пострадавший") },
                { "ранены", (15, "🚑 Раненые / пострадавшие") },
                { "эвакуация", (10, "🚨 Эвакуация / срочная эвакуация") },
                { "военная операция", (30, "⚔️ Упоминание военной операции") },
                { "военные действия", (25, "⚔️ Упоминание военных действий") },
                { "против Ирана", (25, "⚔️ Упоминание военных действий против Ирана") },
                { "конфликт", (20, "⚔️ Упоминание конфликта") },
                { "эскалация", (20, "⚠️ Упоминание эскалации конфликта") },
                { "иран", (15, "🇮🇷 Упоминание Ирана") },
                { "иранский", (15, "🇮🇷 Упоминание иранского влияния") },
                { "иранская угроза", (20, "🇮🇷 Угроза со стороны Ирана") },
                { "иранская ядерная программа", (25, "☢️ Упоминание иранской ядерной программы") },
                { "иранские военные силы", (20, "🎖 Упоминание иранских военных сил") },
                { "иранские боевики", (20, "⚔️ Упоминание иранских боевиков") },
                { "иранская разведка", (15, "🕵️‍♂️ Упоминание иранской разведки") },
                { "иранская поддержка террористов", (25, "⚠️ Упоминание иранской поддержки террористов") },
                { "иранская агрессия", (30, "⚔️ Упоминание иранской агрессии") },
                { "иранская угроза безопасности Израиля", (35, "⚠️ Угроза безопасности Израиля") },
                // Additional Russian keywords
                { "залп", (35, "💥 Залп / массированный обстрел") },
                { "пуск", (35, "🚀 Пуск ракет") },
                { "баллистическая", (35, "☢️ Баллистическая ракета") },
                { "крылатая", (30, "🚀 Крылатая ракета") },
                { "обстрелян", (30, "💥 Под обстрелом / обстрелян") },
                { "обстреляли", (30, "💥 Обстрелян / атакован") },
                { "разрушен", (25, "🏚️ Разрушения / разрушен") },
                { "разрушения", (25, "🏚️ Разрушения") },
                { "потери", (20, "⚠️ Потери / жертвы") },
                { "уничтожили", (25, "💥 Уничтожение / нейтрализация") },

                // Additional English keywords
                { "launch", (35, "🚀 Launch / пуск ракеты") },
                { "rocket attack", (40, "🚀 Rocket attack / ракетная атака") },
                { "missile attack", (40, "🚀 Missile attack / ракетная атака") },
                { "drone", (25, "🛩 Drone / упоминание БПЛА") },
                { "uav", (25, "🛩 UAV / упоминание БПЛА") },
                { "shelling", (30, "💥 Shelling / обстрел") },
                { "bombing", (30, "💣 Bombing / бомбардировка") },
                { "casualties", (25, "⚰️ Casualties / жертвы") },
                { "killed", (25, "⚰️ Killed / погибшие") },
                { "injured", (20, "🚑 Injured / раненые") },

                // Additional Hebrew keywords
                { "שיגור", (35, "🚀 שיגור / запуск") },
                { "ירי", (30, "💥 ירי / огонь") },
                { "פיגוע", (30, "⚠️ פיגוע / теракт") },
                { "הרג", (25, "⚰️ הרג / погибшие") },
                { "נפגע", (20, "🚑 נפגע / ранены") },
                { "פינוי", (20, "🚨 פינוי / эвакуация") },
                { "מל\"ט", (25, "🛩 מל\"ט / БПЛА") },
                { "צבא", (15, "🎖 צבא / войска") },
            };

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; NewsRiskBot/1.0; +https://example.com/bot)");

            int totalScore = 0;
            int newsCount = 0;
            int feedsProcessed = 0;

            foreach (var rssUrl in _rssUrls)
            {
                var feed = await LoadFeedAsync(client, rssUrl, allowHtmlFallback: true);

                if (feed == null)
                {
                    continue;
                }

                feedsProcessed++;

                foreach (var item in feed.Items.Take(10))
                {
                    string title = item.Title?.Text ?? "";
                    string summary = item.Summary?.Text ?? "";
                    string text = title + " " + summary;

                    ScoreText(text, keywords, result, ref totalScore, ref newsCount, title);
                }
            }

            var telegramPosts = await _telegramChannelService.GetLatestPostsAsync(client);

            if (telegramPosts.Count > 0)
            {
                feedsProcessed++;

                foreach (var post in telegramPosts)
                {
                    var shortTitle = post.Length > 100 ? post[..100] + "…" : post;
                    ScoreText(post, keywords, result, ref totalScore, ref newsCount, "[Telegram] " + shortTitle);
                }
            }

            if (newsCount > 0)
            {
                //Расчет риска
                double average = (double)totalScore / newsCount;
                result.Score = (int)(average * 1.8);
            }

            if (result.Score > 100)
            {
                result.Score = 100;
            }

            if (result.Score < 5 && result.Reasons.Count > 0)
            {
                result.Score = 5;
            }

            Console.WriteLine($"Источников обработано: {feedsProcessed}");
            Console.WriteLine($"Новостей/постов: {newsCount}");
            Console.WriteLine($"Сумма баллов: {totalScore}");
            Console.WriteLine($"Итоговый риск: {result.Score}%");

            return result;
        }

        private static void ScoreText(
            string text,
            Dictionary<string, (int Score, string Reason)> keywords,
            RiskResult result,
            ref int totalScore,
            ref int newsCount,
            string titleForLog)
        {
            newsCount++;

            var lowerText = text.ToLowerInvariant();
            int newsScore = 0;

            foreach (var word in keywords)
            {
                if (lowerText.Contains(word.Key.ToLowerInvariant()))
                {
                    newsScore += word.Value.Score;

                    if (!result.Reasons.Contains(word.Value.Reason))
                    {
                        result.Reasons.Add(word.Value.Reason);
                    }
                }
            }

            if (newsScore >= 20)
            {
                result.NewsTitles.Add(titleForLog);
            }

            if (newsScore > 40)
            {
                newsScore = 40;
            }

            totalScore += newsScore;
        }

        private async Task<SyndicationFeed?> LoadFeedAsync(HttpClient client, string url, bool allowHtmlFallback)
        {
            try
            {
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                var body = await response.Content.ReadAsStringAsync();

                if (contentType.Contains("html") || body.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
                {
                    if (!allowHtmlFallback)
                    {
                        Console.WriteLine($"Пропуск ({url}): вернулся HTML, автопоиск фида уже был выполнен ранее");
                        return null;
                    }

                    var discoveredUrl = TryExtractRssLinkFromHtml(body, url);

                    if (discoveredUrl == null)
                    {
                        Console.WriteLine($"Пропуск ({url}): вернулся HTML, RSS-ссылка внутри не найдена");
                        return null;
                    }

                    return await LoadFeedAsync(client, discoveredUrl, allowHtmlFallback: false);
                }

                using var reader = XmlReader.Create(new StringReader(body), SafeXmlSettings);
                return SyndicationFeed.Load(reader);
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга XML ({url}): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка RSS ({url}): {ex.Message}");
                return null;
            }
        }

        private static string? TryExtractRssLinkFromHtml(string html, string pageUrl)
        {
            var match = RssLinkRegex.Match(html);

            if (!match.Success)
            {
                return null;
            }

            var href = match.Groups[2].Value;

            if (Uri.TryCreate(new Uri(pageUrl), href, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            return null;
        }
    }
}