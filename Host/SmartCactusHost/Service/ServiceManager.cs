using LoggerService;
using Microsoft.Extensions.Configuration;
using Repository.Contracts;
using Service.Contracts;

namespace Service;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<ITelegramBotService> _telegramBotService;
    private readonly Lazy<IMQTTBrokerService> _MQTTBrokerService;

    public ServiceManager(ILogger logger, IRepositoryManager repositoryManager, IConfiguration configuration)
    {
        _telegramBotService = new(() => new TelegramBotService(logger, repositoryManager, configuration));
        _MQTTBrokerService = new(() => new MQTTBrokerService(logger));
    }

    public ITelegramBotService TelegramBotService => _telegramBotService.Value;
    public IMQTTBrokerService MQTTBrokerService => _MQTTBrokerService.Value;
}
