using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using NLog;

namespace SCConnector;

internal class Program
{
    private static ILogger? _logger;
    static async Task Main()
    {
        InitLogger();

        var broker = MQTTBroker.Core.MQTTBroker.InitializeInstance(GetMqttBrokerCertificate(), _logger);
        var telegramBot = TelegramBot.Core.TelegramBot.InitializeInstance(GetTelegramApiKey(), _logger);

        //telegramBot.PublishToBroker = broker.AcceptPublish;
        //telegramBot.SubcribeTopicInternal = broker.TopicSubscribed;

        //broker.MessageRecieved = telegramBot.AcceptMessage;
        
        await broker.StartServer();
        telegramBot.StartRecieving();
        
        await Task.Delay(-1);
    }

    static void InitLogger()
    {
        LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
        });
        _logger = LogManager.GetCurrentClassLogger();
    }
    static string GetTelegramApiKey()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("private/appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        return config["TelegramBotApiKey"]?.ToString() ??
            throw new Exception("TelegramBotApiKey not found");
    }
    static X509Certificate2 GetMqttBrokerCertificate()
    {
        var certificatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "private/certificate.pfx");
        var certificatePassword = "hello";

        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"Sertificate not found: {certificatePath}");
        }

        return new(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);
    }
}
