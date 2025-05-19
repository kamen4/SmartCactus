namespace Repository.Contracts;

public interface IRepositoryManager
{
    IUserRepository User { get; }
    IDeviceRepository Device { get; }
    ITopicRepository Topic { get; }
    IDeviceTopicRepository DeviceTopic { get; }
    IMessageRepository Message { get; }
    ITelegramBrokerActionRepository TelegramBrokerAction { get; }
    void Save();
}
