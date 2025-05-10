using Entities.Enums;

namespace WebApp.Models;

public class MqttDeviceViewModel
{
    public Guid Id { get; set; }
    public string MqttClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DeviceType DeviceType { get; set; }
    public DateTime CreatedOn { get; set; }
}
