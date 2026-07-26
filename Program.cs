using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramBot.Models;
using TelegramBot.Services;
using VANewsBot.Models.TelegramBot.Models;


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



// create shared services for monitoring and command handling
var riskSection = config.GetSection("RiskMonitoring");

var riskSettings = new RiskMonitoringSettings();

if (riskSection.Exists())
{
    var rssChildren = riskSection.GetSection("RssUrls").GetChildren();
    foreach (var c in rssChildren)
    {
        if (!string.IsNullOrEmpty(c.Value))
            riskSettings.RssUrls.Add(c.Value);
    }

    var chanChildren = riskSection.GetSection("TelegramChannels").GetChildren();
    foreach (var c in chanChildren)
    {
        if (!string.IsNullOrEmpty(c.Value))
            riskSettings.TelegramChannels.Add(c.Value);
    }

    riskSettings.IsraeliProxyUrl = riskSection["IsraeliProxyUrl"];
    // optional poll interval
    if (int.TryParse(riskSection["PollIntervalSeconds"], out var iv))
    {
        // validate and enforce minimum of 5 seconds
        riskSettings.PollIntervalSeconds = iv >= 5 ? iv : 5;
    }
}

var orefAlertService = new OrefAlertService(riskSettings.IsraeliProxyUrl);
var telegramChannelService = new TelegramChannelService(riskSettings.TelegramChannels);
var newsRiskService = new NewsRiskService(riskSettings.RssUrls, orefAlertService, telegramChannelService);

var commandHandler = new CommandHandlerService(botClient, newsRiskService);



long chatId = 0;


if (!string.IsNullOrEmpty(
    config["Telegram:ChatId"]))
{
    chatId =
        long.Parse(
            config["Telegram:ChatId"]!
        );
}



// (RiskMonitoring already bound above)

if (riskSettings.RssUrls.Count == 0 && riskSettings.TelegramChannels.Count == 0)
{
    Console.WriteLine(
        "Предупреждение: секция RiskMonitoring пустая или не найдена в appsettings.json"
    );
}


if (chatId != 0)
{
    var warMonitor = new WarMonitorService(botClient, chatId, newsRiskService, riskSettings.PollIntervalSeconds);

    _ = Task.Run(() => warMonitor.Start());
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