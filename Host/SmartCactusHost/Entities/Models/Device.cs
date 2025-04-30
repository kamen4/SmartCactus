using Entities.Enums;

namespace Entities.Models;

public class Device
{
    public Guid Id { get; set; }
    public string? MqttClientId { get; set; }
    public string? MqttUsername { get; set; }
    public string? MqttPassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    public List<Topic> Topics { get; set; } = [];
}
