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
            TelegramBotClient bot)
        {
            _bot = bot;
            _riskService = new NewsRiskService();
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
                var result =
                    await _riskService.CalculateRisk();



                string reasons =
                    string.Join(
                        "\n",
                        result.Reasons
                            .Select(
                                x => "• " + x
                            )
                    );



                string message =
                    $"📊 Текущий мониторинг\n\n" +

                    $"🇮🇱 Израиль – 🇮🇷 Иран\n\n" +

                    $"Риск: {result.Score}%\n\n" +

                    $"Причины:\n{reasons}\n\n" +

                    $"Время: {DateTime.Now:dd.MM.yyyy HH:mm}";



                await _bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: message,
                    cancellationToken: cancellationToken
                );
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