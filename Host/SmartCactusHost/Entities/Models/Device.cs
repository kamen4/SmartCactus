using Entities.Enums;

namespace Entities.Models;

public class Device
{
    public Guid Id { get; set; }
    public string? MqttClientId { get; set; }
    public string? MqttUsername { get; set; }
    public string? MqttPasswordHash { get; set; }
    public bool IsActive { get; set; } = false;
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
    public DateTime CreatedOn { get; set; } = DateTime.Now;


    public List<Topic> Topics { get; set; } = [];
}
