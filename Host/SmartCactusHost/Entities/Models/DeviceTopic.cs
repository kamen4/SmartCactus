using Entities.Enums;

namespace Entities.Models;

public class DeviceTopic
{
    public EventType EventType { get; set; } = EventType.None;

    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
