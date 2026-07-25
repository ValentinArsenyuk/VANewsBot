using Telegram.Bot;
using TelegramBot.Models;

namespace TelegramBot.Services
{
    public class WarMonitorService
    {
        private readonly ITelegramBotClient _bot;
        private readonly long _chatId;
        private readonly NewsRiskService _newsService;

        private int? _lastRisk = null;


        public WarMonitorService(
            ITelegramBotClient bot,
            long chatId)
        {
            _bot = bot;
            _chatId = chatId;

            _newsService = new NewsRiskService();
        }

        public async Task Start()
        {
            Console.WriteLine(
                "Мониторинг Израиль–Иран запущен..."
            );


            while (true)
            {
                try
                {
                    RiskResult riskResult =
                        await _newsService.CalculateRisk();


                    int risk =
                        riskResult.Score;



                    Console.WriteLine(
                        $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: Risk = {risk}%"
                    );


                    foreach (var reason in riskResult.Reasons)
                    {
                        Console.WriteLine(
                            "   → " + reason
                        );
                    }

                    // Первый запуск или переход в критическую зону

                    if (risk >= 50 && (!_lastRisk.HasValue || _lastRisk.Value < 50))
                    {
                        await SendAlert(riskResult);
                    }
                    else if (_lastRisk.HasValue &&
                             risk - _lastRisk.Value >= 20)
                    {
                        await SendAlert(riskResult);
                    }

                    _lastRisk = risk;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "Ошибка мониторинга: " +
                        ex.Message
                    );
                }



                // Проверка каждые 5 минут

                await Task.Delay(
                    TimeSpan.FromSeconds(30)
                );
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
            string reasons;


            if (result.Reasons.Count > 0)
            {
                reasons = string.Join(
                    "\n",
                    result.Reasons.Select(
                        x => "• " + x
                    )
                );
            }
            else
            {
                reasons = "• Причины не определены";
            }

            string news;

            if (result.NewsTitles.Count > 0)
            {
                news = string.Join(
                    "\n",
                    result.NewsTitles
                        .Take(5)
                        .Select(
                            x => "📰 " + x
                        )
                );
            }
            else
            {
                news = "Нет важных новостей";
            }

            int change = 0;


            if (_lastRisk.HasValue)
            {
                change =
                    result.Score -
                    _lastRisk.Value;
            }

            string changeText =
                change > 0
                ? $"+{change}%"
                : $"{change}%";

            string message =
                $"🚨 ВНИМАНИЕ!\n\n" +

                $"🇮🇱 Израиль – 🇮🇷 Иран\n\n" +

                $"Индекс напряженности: {result.Score}%\n" +

                $"Изменение: {changeText}\n\n" +

                $"Уровень: {GetRiskLevel(result.Score)}\n\n" +

                $"Причины:\n{reasons}\n\n" +

                $"Новости:\n{news}\n\n" +

                $"Время: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n" +

                $"Проверьте последние новости.";



            await _bot.SendMessage(
                chatId: _chatId,
                text: message
            );


            Console.WriteLine(
                "⚠ Отправлено предупреждение"
            );
        }
    }
}