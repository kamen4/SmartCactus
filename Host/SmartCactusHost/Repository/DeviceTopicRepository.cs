using Entities.Models;
using Repository.Contracts;

namespace Repository;

public class DeviceTopicRepository : RepositoryBase<DeviceTopic>, IDeviceTopicRepository
{
    public DeviceTopicRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateConnection(DeviceTopic connection)
    {
        Create(connection);
    }

    public void DeleteConnection(DeviceTopic connection)
    {
        Delete(connection);
    }

    public IEnumerable<DeviceTopic> GetAllConnections(bool trackChanges)
    {
        return FindAll(trackChanges).ToList();
    }

    public DeviceTopic? GetConnectionByDeviceTopic(Guid deviceId, Guid topicId, bool trackChanges)
    {
        return FindByCondition(dt => dt.TopicId == topicId && dt.DeviceId == deviceId, trackChanges).SingleOrDefault();
    }

    public IEnumerable<DeviceTopic> GetConnectionsByDevice(Guid deviceId, bool trackChanges)
    {
        return FindByCondition(dt => dt.DeviceId == deviceId, trackChanges).ToList();
    }

    public IEnumerable<DeviceTopic> GetConnectionsByTopic(Guid topicId, bool trackChanges)
    {
        return FindByCondition(dt => dt.TopicId == topicId, trackChanges).ToList();
    }
}
