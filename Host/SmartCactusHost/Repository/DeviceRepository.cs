using Repository.Contracts;
using Entities.Models;
using System.Linq.Expressions;

namespace Repository;

public class DeviceRepository : RepositoryBase<Device>, IDeviceRepository
{
    public DeviceRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateDevice(Device device)
    {
        Create(device);
    }

    public void DeleteDevice(Device device)
    {
        Delete(device);
    }

    public IEnumerable<Device> GetAllDevices(bool trackChanges)
    {
        return FindAll(trackChanges).OrderBy(d => d.MqttClientId).ToList();
    }

    public Device? GetDevice(Guid deviceId, bool trackChanges)
    {
        return FindByCondition(d => d.Id.Equals(deviceId), trackChanges).SingleOrDefault();
    }

    public Device? GetDeviceByCondition(Expression<Func<Device, bool>> expression, bool trackChanges)
    {
        return FindByCondition(expression, trackChanges).SingleOrDefault();
    }

    public Device? GetDeviceByMqttUsername(string username, bool trackChanges)
    {
        return FindByCondition(d => d.MqttUsername == username, trackChanges).SingleOrDefault();
    }
}
