using System.Text.Json.Serialization;

namespace Entities.DTO.Topics;

public class PingResponseDTO
{
    [JsonIgnore]
    public string? DeviceId { get; set; }
    [JsonPropertyName("subcsriptions")]
    public List<TopicDTO> Subcsriptions { get; set; } = [];
    [JsonPropertyName("publications")]
    public List<TopicDTO> Publications { get; set; } = [];
}
