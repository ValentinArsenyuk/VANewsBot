using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramBot.Services;


Console.OutputEncoding = System.Text.Encoding.UTF8;


var config =
    new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(
            "appsettings.json",
            optional: false,
            reloadOnChange: true
        )
        .Build();



var token =
    config["Telegram:Token"];


if (string.IsNullOrEmpty(token))
{
    Console.WriteLine(
        "Telegram Token не найден!"
    );

    return;
}



var botClient =
    new TelegramBotClient(token);



var commandHandler =
    new CommandHandlerService(botClient);



long chatId = 0;


if (!string.IsNullOrEmpty(
    config["Telegram:ChatId"]))
{
    chatId =
        long.Parse(
            config["Telegram:ChatId"]!
        );
}



if (chatId != 0)
{
    var warMonitor =
        new WarMonitorService(
            botClient,
            chatId
        );


    _ = Task.Run(() =>
        warMonitor.Start()
    );
}



Console.WriteLine(
    "Бот @VANewsBot запущен."
);



botClient.StartReceiving(

    async (bot, update, ct) =>
    {

        if (update.Message != null)
        {
            Console.WriteLine(
                $"ChatId: {update.Message.Chat.Id}"
            );
        }


        await commandHandler.Handle(
            update,
            ct
        );
    },


    (bot, exception, ct) =>
    {
        Console.WriteLine(
            $"Ошибка Telegram: {exception.Message}"
        );

        return Task.CompletedTask;
    }

);



Console.WriteLine(
    "Ожидание сообщений..."
);



Console.ReadLine();