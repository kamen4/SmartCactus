using System.ComponentModel.DataAnnotations;

namespace Entities.Models;

public class Topic
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JsonShema { get; set; } = string.Empty;

    public List<Device> Devices { get; set; } = [];
}
