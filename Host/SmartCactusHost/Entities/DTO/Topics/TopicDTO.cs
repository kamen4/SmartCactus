using System.Text.Json.Serialization;

namespace Entities.DTO.Topics;

public class TopicDTO
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
    [JsonPropertyName("jsonSchema")]
    public string JsonSchema { get; set; } = string.Empty;
}
