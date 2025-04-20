using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using LoggerService;
using Microsoft.Extensions.Configuration;
using Service.Contracts;

namespace Service;

public class MQTTBrokerService : IMQTTBrokerService
{
    private readonly ILogger _logger;
    private readonly MQTTBroker.MQTTBroker _broker;

    public MQTTBrokerService(ILogger logger)
    {
        _logger = logger;
        _broker = MQTTBroker.MQTTBroker.InitializeInstance(GetMqttBrokerCertificate(), _logger);
    }
    static X509Certificate2 GetMqttBrokerCertificate()
    {
        var certificatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "private/certificate.pfx");
        var certificatePassword = "hello";

        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"Sertificate not found: {certificatePath}");
        }

        return new(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);
    }

    public void StartBroker()
    {
        _broker.StartServer();
    }
}
