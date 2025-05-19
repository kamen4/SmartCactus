using Entities.Models;

namespace Repository.Contracts;

public interface IMessageRepository
{
    IEnumerable<MqttMessage> GetAllMessages(bool trackChanges);
    MqttMessage? GetMessage(Guid messageId, bool trackChanges);
    MqttMessage? GetLastMessageInTopic(Topic topic, bool trackChanges);
    void CreateMessage(MqttMessage message);
    void DeleteMessage(MqttMessage message);
}
