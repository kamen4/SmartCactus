using System.Linq.Expressions;
using Entities.Models;

namespace Repository.Contracts;

public interface IDeviceRepository
{
    IEnumerable<Device> GetAllDevices(bool trackChanges);
    Device? GetDevice(Guid deviceId, bool trackChanges);
    Device? GetDeviceByMqttUsername(string username, bool trackChanges);
    Device? GetDeviceByClientId(string clientId, bool trackChanges);
    void CreateDevice(Device device);
    void DeleteDevice(Device device);
}
