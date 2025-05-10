using Entities.Models;
using Repository.Contracts;

namespace Repository;

public class MessageRepository : RepositoryBase<MqttMessage>, IMessageRepository
{
    public MessageRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateMessage(MqttMessage message)
    {
        Create(message);
    }

    public void DeleteMessage(MqttMessage message)
    {
        Delete(message);
    }

    public IEnumerable<MqttMessage> GetAllMessages(bool trackChanges)
    {
        return FindAll(trackChanges).OrderBy(m => m.Payload).ToList();
    }

    public MqttMessage? GetMessage(Guid messageId, bool trackChanges)
    {
        return FindByCondition(m => m.Id.Equals(messageId), trackChanges).SingleOrDefault();
    }
}
