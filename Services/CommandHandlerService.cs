using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Services
{
    public class CommandHandlerService
    {
        private readonly TelegramBotClient _bot;
        private readonly NewsRiskService _riskService;

        public CommandHandlerService(
            TelegramBotClient bot,
            NewsRiskService newsRiskService)
        {
            _bot = bot;
            _riskService = newsRiskService;
        }



        public async Task Handle(
            Update update,
            CancellationToken cancellationToken)
        {

            if (update.Message == null)
                return;


            string? text =
                update.Message.Text;


            if (string.IsNullOrEmpty(text))
                return;



            if (text == "/risk")
            {
                // register subscriber
                var chatId = update.Message.Chat.Id;
                string displayName;

                if (update.Message.From != null)
                {
                    var from = update.Message.From;
                    if (!string.IsNullOrEmpty(from.Username))
                        displayName = "@" + from.Username;
                    else
                        displayName = (from.FirstName + " " + from.LastName).Trim();
                }
                else
                {
                    displayName = chatId.ToString();
                }

                SubscriberStore.Add(
                    chatId,
                    displayName,
                    phone: null,
                    username: update.Message.From?.Username,
                    firstName: update.Message.From?.FirstName,
                    lastName: update.Message.From?.LastName,
                    language: update.Message.From?.LanguageCode,
                    chatType: update.Message.Chat?.Type.ToString());

                var result = await _riskService.CalculateRisk();

                string reasons = string.Join("\n", result.Reasons.Select(x => "• " + x));

                string subscribers = SubscriberStore.GetDisplayListText();

                string message =
                    $"📊 Текущий мониторинг\n\n" +
                    $"🇮🇱 Израиль – 🇮🇷 Иран\n\n" +
                    $"Риск: {result.Score}%\n\n" +
                    $"Причины:\n{reasons}\n\n" +
                    //$"Подписались: {subscribers}\n\n" +
                    $"Время: {DateTime.Now:dd.MM.yyyy HH:mm}";

                var subscriberIds = SubscriberStore.GetSubscriberIds();

                if (subscriberIds.Count == 0)
                {
                    await _bot.SendMessage(chatId: chatId, text: message, cancellationToken: cancellationToken);
                }
                else
                {
                    foreach (var sid in subscriberIds)
                    {
                        try
                        {
                            await _bot.SendMessage(chatId: sid, text: message, cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка отправки подписчику {sid}: {ex.Message}");
                        }
                    }
                }
            }

            // set phone: /setphone +7926xxxxxxx
            if (text.StartsWith("/setphone"))
            {
                var cid = update.Message.Chat.Id;
                var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    await _bot.SendMessage(chatId: cid, text: "Использование: /setphone <номер>", cancellationToken: cancellationToken);
                }
                else
                {
                    var phone = parts[1].Trim();
                    SubscriberStore.SetPhone(cid, phone);
                    await _bot.SendMessage(chatId: cid, text: $"Сохранён номер: {phone}", cancellationToken: cancellationToken);
                }
            }



            if (text == "/start")
            {
                var chatId = update.Message.Chat.Id;


                await _bot.SendMessage(
                    chatId: chatId,
                    text: $"Ваш ChatId: {chatId}",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}