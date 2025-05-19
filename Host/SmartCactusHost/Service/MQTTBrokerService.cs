using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Entities.Models;
using LoggerService;
using Repository.Contracts;
using Service.Contracts;
using System.Security.Cryptography;
using Utils;
using Entities.DTO;
using Entities.DTO.Topics;
using Entities.Enums;

namespace Service;

public class MQTTBrokerService : IMQTTBrokerService
{
    private readonly ILogger _logger;
    private readonly IRepositoryManager _repositoryManager;
    private readonly MQTTBroker.MQTTBroker _broker;
    private readonly RandomNumberGenerator _rng;

    public MQTTBrokerService(ILogger logger, IRepositoryManager repositoryManager)
    {
        _logger = logger;
        _repositoryManager = repositoryManager;
        _broker = MQTTBroker.MQTTBroker.InitializeInstance(GetMqttBrokerCertificate(), _logger);
        _rng = RandomNumberGenerator.Create();

        _broker.AuthorizeDevice += AuthorizeDevice;
        _broker.UpdateDeviceTopics += UpdateDeviceTopics;
        _broker.MarkAsDisconnected += MarkAsDisconnected;
        _broker.MessagePublished += MessagePublished;
    }

    public void StartBroker() => _broker.StartServer();

    public void StopBroker() => _broker.StopServer();

    public bool IsStarted() => _broker.IsStarted();

    public void Ping() => _broker.Ping();

    public Task Publish(string topic, string payload) => _broker.Publish(topic, payload);

    public string RequestDeviceCreation()
    {
        MqttSettingsDTO request = new()
        {
            Host = GetLocalIPAddress(),
            Port = _broker.Port,
            Username = Guid.NewGuid().ToString(),
            Password = PasswordUtils.GeneratePassword(16)
        };

        Device device = new()
        {
            MqttUsername = request.Username,
            MqttPasswordHash = Convert.ToBase64String(PasswordUtils.Hash(request.Password, _rng))
        };
        _repositoryManager.Device.CreateDevice(device);
        _repositoryManager.Save();

        var json = JsonSerializer.Serialize(request);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return base64;
    }

    private Device? AuthorizeDevice(string clientId, string username, string password)
    {
        var device = _repositoryManager.Device.GetDeviceByMqttUsername(username, trackChanges: true);
        if (device == null)
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} not found.");
            return null;
        }
        if (string.IsNullOrEmpty(device.MqttClientId) && DateTime.Now - device.CreatedOn >= TimeSpan.FromMinutes(3d))
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} is expired.");
            return null;
        }
        if (!PasswordUtils.Verify(Convert.FromBase64String(device.MqttPasswordHash ?? ""), password))
        {
            _logger.Error($"MQTTBrokerServ|Invalid password for device {username}.");
            return null;
        }
        if (!string.IsNullOrEmpty(device.MqttClientId) && device.MqttClientId != clientId)
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} already exist.");
            return null;
        }
        if (string.IsNullOrEmpty(device.MqttClientId))
        {
            device.MqttClientId = clientId;
            _repositoryManager.Save();
        }
        return device;
    }

    private void UpdateDeviceTopics(PingResponseDTO pingResponse)
    {
        var device = _repositoryManager.Device.GetDeviceByClientId(pingResponse.DeviceId ?? "", true);
        if (device is null)
        {
            _logger.Error($"MQTTBrokerServ|Device with clentId {pingResponse.DeviceId} not found.");
            return;
        }
        
        pingResponse.Publications.ForEach(t => EnsureTopicCreated(t, device, EventType.Publication));
        pingResponse.Subcsriptions.ForEach(t => EnsureTopicCreated(t, device, EventType.Subscription));

        var allActiveTopics = pingResponse.Publications.Concat(pingResponse.Subcsriptions);

        device.Topics
            .ExceptBy(allActiveTopics.Select(t => t.Topic), t => t.Name)
            .ToList()
            .ForEach(topic =>
            {
                var connection = _repositoryManager.DeviceTopic.GetConnectionByDeviceTopic(device.Id, topic.Id, false);
                if (connection is not null)
                {
                    _repositoryManager.DeviceTopic.DeleteConnection(connection);
                }
            });

        device.IsActive = true;
        if (pingResponse.Subcsriptions.Count > 0)
        {
            device.DeviceType |= DeviceType.Output;
        }
        if (pingResponse.Publications.Count > 0)
        {
            device.DeviceType |= DeviceType.Sensor;
        }

        _repositoryManager.Save();
    }

    private void EnsureTopicCreated(TopicDTO topicDto, Device device, EventType eventType)
    {
        var topic = _repositoryManager.Topic.GetTopicByName(topicDto.Topic, false);
        if (topic is null)
        {
            topic = new()
            {
                Id = Guid.NewGuid(),
                Name = topicDto.Topic,
                JsonShema = topicDto.JsonSchema
            };
            _repositoryManager.Topic.CreateTopic(topic);
        }

        var connection = _repositoryManager.DeviceTopic.GetConnectionByDeviceTopic(device.Id, topic.Id, false);
        if (connection is null)
        {
            _repositoryManager.DeviceTopic.CreateConnection(new()
            {
                DeviceId = device.Id,
                TopicId = topic.Id,
                EventType = eventType
            });
        }
        _repositoryManager.Save();
    }

    private void MarkAsDisconnected(string clientId)
    {
        var device = _repositoryManager.Device.GetDeviceByClientId(clientId, true);
        if (device is null)
        {
            _logger.Error($"MQTTBrokerServ|Device with clentId {clientId} not found.");
            return;
        }
        device.IsActive = false;
        _repositoryManager.Save();
    }

    private void MessagePublished(string clientId, string topicName, string payload)
    {
        var device = _repositoryManager.Device.GetDeviceByClientId(clientId, false);
        if (device is null)
        {
            _logger.Error($"MQTTBrokerServ|Device with clentId {clientId} not found.");
            return;
        }

        var topic = _repositoryManager.Topic.GetTopicByName(topicName, false);
        if (topic is null)
        {
            _logger.Error($"MQTTBrokerServ|Topic with name {topicName} not found.");
            _ = _broker.StartPing(clientId);
            return;
        }

        _repositoryManager.Message.CreateMessage(new()
        {
            DeviceId = device.Id,
            TopicId = topic.Id,
            Payload = payload
        });
        _repositoryManager.Save();
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private static X509Certificate2 GetMqttBrokerCertificate()
    {
        var certificatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "private/certificate.pfx");
        var certificatePassword = "hello";

        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"Sertificate not found: {certificatePath}");
        }

        return new(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);
    }
}
