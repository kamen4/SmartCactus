using System.ComponentModel.DataAnnotations;

namespace Entities.Models;

public class MqttMessage
{
    [Key]
    public Guid Id { get; set; }

    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
