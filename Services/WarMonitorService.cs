using Telegram.Bot;
using TelegramBot.Models;
using VANewsBot.Models.TelegramBot.Models;

namespace TelegramBot.Services
{
    public class WarMonitorService
    {
        private readonly ITelegramBotClient _bot;
        private readonly long _chatId;
        private readonly NewsRiskService _newsService;
        private readonly int _pollIntervalSeconds;

        private int? _lastRisk = null;


        public WarMonitorService(
            ITelegramBotClient bot,
            long chatId,
            NewsRiskService newsService,
            int pollIntervalSeconds = 5)
        {
            _bot = bot;
            _chatId = chatId;
            _newsService = newsService;
            // enforce minimum poll interval of 5 seconds
            _pollIntervalSeconds = pollIntervalSeconds >= 5 ? pollIntervalSeconds : 5;
        }

        public async Task Start()
        {
            Console.WriteLine("Мониторинг Израиль–Иран запущен...");

            while (true)
            {
                try
                {
                    RiskResult riskResult = await _newsService.CalculateRisk();
                    int risk = riskResult.Score;

                    Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: Risk = {risk}%");

                    foreach (var reason in riskResult.Reasons)
                    {
                        Console.WriteLine("   → " + reason);
                    }

                    // do not send full report every check anymore

                    if (risk >= 50 && (!_lastRisk.HasValue || _lastRisk.Value < 50))
                    {
                        await SendAlert(riskResult);
                    }
                    else if (_lastRisk.HasValue && risk - _lastRisk.Value >= 20)
                    {
                        await SendAlert(riskResult);
                    }

                    _lastRisk = risk;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка мониторинга: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds));
            }
        }

        private string GetRiskLevel(int score)
        {
            return score switch
            {
                < 30 => "🟢 Низкий",
                < 60 => "🟡 Повышенный",
                < 80 => "🟠 Высокий",
                _ => "🔴 Критический"
            };
        }

        private async Task SendAlert(RiskResult result)
        {
            string reasons = result.Reasons.Count > 0
                ? string.Join(" / ", result.Reasons)
                : "Причины не определены";

            string news = result.NewsTitles.Count > 0
                ? string.Join("\n", result.NewsTitles.Take(5).Select(x => "📰 " + x))
                : "Нет важных новостей";

            int change = _lastRisk.HasValue ? result.Score - _lastRisk.Value : 0;
            string changeText = change > 0 ? $"+{change}%" : $"{change}%";

            string message =
                $"🇮🇱 Израиль – 🇮🇷 Иран\n\n" +
                $"🚨 ВНИМАНИЕ!\n\n" +
                $"Индекс напряженности: {result.Score}%\n" +
                $"Изменение: {changeText}\n\n" +
                $"Уровень: {GetRiskLevel(result.Score)}\n\n" +
                $"Причины: {reasons} / " +
                $"Новости:\n{news}\n\n" +
                $"Время: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n" +
                $"Проверьте последние новости.";

            var subscriberIds = TelegramBot.Services.SubscriberStore.GetSubscriberIds();

            // send to all subscribers
            foreach (var sid in subscriberIds)
            {
                try
                {
                    await _bot.SendMessage(chatId: sid, text: message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки подписчику {sid}: {ex.Message}");
                }
            }

            // always also send to configured admin chat
            try
            {
                if (!subscriberIds.Contains(_chatId))
                {
                    await _bot.SendMessage(chatId: _chatId, text: message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка отправки в админ-чат: " + ex.Message);
            }

            Console.WriteLine("⚠ Отправлено предупреждение подписчикам и администратору");
        }

        private async Task SendReport(RiskResult result)
        {
            string reasons = result.Reasons.Count > 0
                ? string.Join(" / ", result.Reasons)
                : "Причины не определены";

            string news = result.NewsTitles.Count > 0
                ? string.Join("\n", result.NewsTitles.Take(10).Select(x => "📰 " + x))
                : "Нет важных новостей";

            string message =
                $"📊 Мониторинг — полный отчёт\n\n" +
                $"🇮🇱 Израиль – 🇮🇷 Иран\n\n" +
                $"Риск: {result.Score}%\n\n" +
                $"Причины: {reasons} / " +
                $"Новости:\n{news}\n\n" +
                $"Время: {DateTime.Now:dd.MM.yyyy HH:mm}";

            await _bot.SendMessage(chatId: _chatId, text: message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }
    }
}