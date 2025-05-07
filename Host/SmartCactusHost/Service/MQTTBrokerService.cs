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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Utils;
using Entities.DTO;

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

    private Device? AuthorizeDevice(string clientId, string username, string password)
    {
        var device = _repositoryManager.Device.GetDeviceByMqttUsername(username, trackChanges: true);
        if (device == null)
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} not found.");
            return null;
        }
        if (DateTime.Now - device.CreatedOn >= TimeSpan.FromMinutes(3d))
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} is expired.");
            return null;
        }
        if (!PasswordsUtil.Verify(Convert.FromBase64String(device.MqttPasswordHash ?? ""), password))
        {
            _logger.Error($"MQTTBrokerServ|Invalid password for device {username}.");
            return null;
        }
        if (device.MqttClientId is not null && device.MqttClientId != clientId)
        {
            _logger.Error($"MQTTBrokerServ|Device with username {username} already exist.");
            return null;
        }
        //ok
        if (string.IsNullOrEmpty(device.MqttClientId))
        {
            device.MqttClientId = clientId;
            _repositoryManager.Save();
        }
        return device;
    }

    public void StartBroker() => _broker.StartServer();

    public void StopBroker() => _broker.StopServer();

    public bool IsStarted() => _broker.IsStarted();

    public string RequestDeviceCreation()
    {
        DeviceCreationRequest request = new()
        {
            Host = GetLocalIPAddress(),
            Port = _broker.Port,
            Username = Guid.NewGuid().ToString(),
            Password = PasswordsUtil.GeneratePassword(16)
        };

        Device device = new()
        {
            MqttUsername = request.Username,
            MqttPasswordHash = Convert.ToBase64String(PasswordsUtil.Hash(request.Password, _rng))
        };
        _repositoryManager.Device.CreateDevice(device);
        _repositoryManager.Save();

        var json = JsonSerializer.Serialize(request);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return base64;
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
}
