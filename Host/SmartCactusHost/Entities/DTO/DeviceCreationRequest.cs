using System.Text.Json.Serialization;

namespace Entities.DTO;

public class DeviceCreationRequest
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
    [JsonPropertyName("port")]
    public int Port { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
