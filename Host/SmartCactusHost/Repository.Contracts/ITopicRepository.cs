using Entities.Models;

namespace Repository.Contracts;

public interface ITopicRepository
{
    IEnumerable<Topic> GetAllTopics(bool trackChanges);
    Topic? GetTopic(Guid topicId, bool trackChanges);
    Topic? GetTopicByName(string name, bool trackChanges);
    void CreateTopic(Topic topic);
    void DeleteTopic(Topic topic);
}
