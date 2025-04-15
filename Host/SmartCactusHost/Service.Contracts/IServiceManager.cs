namespace Service.Contracts;

public interface IServiceManager
{
    ITelegramBotService TelegramBotService { get; }
    IMQTTBrokerService MQTTBrokerService { get; }
}
