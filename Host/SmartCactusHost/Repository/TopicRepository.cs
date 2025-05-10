using Entities.Models;
using Repository.Contracts;

namespace Repository;

public class TopicRepository : RepositoryBase<Topic>, ITopicRepository
{
    public TopicRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateTopic(Topic topic)
    {
        Create(topic);
    }

    public void DeleteTopic(Topic topic)
    {
        Delete(topic);
    }

    public IEnumerable<Topic> GetAllTopics(bool trackChanges)
    {
        return FindAll(trackChanges).OrderBy(t => t.Name).ToList();
    }

    public Topic? GetTopic(Guid topicId, bool trackChanges)
    {
        return FindByCondition(t => t.Id.Equals(topicId), trackChanges).SingleOrDefault();
    }

    public Topic? GetTopicByName(string name, bool trackChanges)
    {
        return FindByCondition(t => t.Name.Equals(name), trackChanges).SingleOrDefault();
    }
}
