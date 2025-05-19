using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.Models;

public class TelegramBrokerAction
{
    [Key]
    public Guid Id { get; set; }
    public EventType EventType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string? Selector { get; set; }
}
