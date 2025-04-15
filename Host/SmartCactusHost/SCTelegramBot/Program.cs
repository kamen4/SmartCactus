using Microsoft.Extensions.Configuration;
using NLog;

namespace SCTelegramBot;

internal class Program
{
    private static ILogger? _logger;

    static async Task Main()
    {
        InitLogger();

        _ = TelegramBot.InitializeInstance(ApiKey, _logger);

        await Task.Delay(-1);
    }
    static void InitLogger()
    {
        LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
        });
        _logger = LogManager.GetCurrentClassLogger();
    }
    static string ApiKey => 
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build()["TelegramBotApiKey"]?.ToString() ??
            throw new Exception("TelegramBotApiKey not found");
}