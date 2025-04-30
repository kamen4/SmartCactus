namespace Entities.Models;

public class Topic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Device> Devices { get; set; } = [];
}
