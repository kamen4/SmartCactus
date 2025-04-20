using LoggerService;
using Microsoft.Extensions.Configuration;
using Service.Contracts;

namespace Service;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger _logger;
    private readonly TelegramBot.TelegramBot _bot;

    public TelegramBotService(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;
        _bot = TelegramBot.TelegramBot.InitializeInstance(configuration["telegram:api_key"] ?? "", _logger);
    }

    public void StartBot()
    {
        _bot.StartRecieving();
    }
}
