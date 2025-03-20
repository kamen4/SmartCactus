using Microsoft.Extensions.Configuration;

namespace SCTelegramBot;

internal class Program
{
    static string? _apiKey;
    static void Main(string[] args)
    {
        InitVariables();
    }

    static void InitVariables()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _apiKey = configuration["TelegramBotApiKey"]?.ToString() ??
            throw new Exception("TelegramBotApiKey not found");
    }
}
