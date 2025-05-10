using Entities.Models;

namespace Repository.Contracts;

public interface IDeviceTopicRepository
{
    IEnumerable<DeviceTopic> GetAllConnections(bool trackChanges);
    IEnumerable<DeviceTopic> GetConnectionsByDevice(Guid deviceId, bool trackChanges);
    IEnumerable<DeviceTopic> GetConnectionsByTopic(Guid topicId, bool trackChanges);
    DeviceTopic? GetConnectionByDeviceTopic(Guid deviceId, Guid topicId, bool trackChanges);

    void CreateConnection(DeviceTopic connection);
    void DeleteConnection(DeviceTopic connection);
}
