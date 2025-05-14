using Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Repository.Contracts;
using Service.Contracts;
using WebApp.Models;
using ILogger = LoggerService.ILogger;

namespace WebApp.Controllers;

public class MQTTController : Controller
{
    private readonly ILogger _logger;
    private readonly IServiceManager _serviceManager;
    private readonly IRepositoryManager _repositoryManager;

    public MQTTController(ILogger logger, IServiceManager serviceManager, IRepositoryManager repositoryManager)
    {
        _logger = logger;
        _serviceManager = serviceManager;
        _repositoryManager = repositoryManager;
    }

    public IActionResult Index()
    {
        MqttViewModel vm = new()
        {
            IsStarted = _serviceManager.MQTTBrokerService.IsStarted()
        };

        return View(vm);
    }

    public IActionResult StartMQTTBroker()
    {
        _serviceManager.MQTTBrokerService.StartBroker();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult StopMQTTBroker()
    {
        _serviceManager.MQTTBrokerService.StopBroker();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Devices()
    {
        var devices = _repositoryManager.Device.GetAllDevices(false);
        var vm = devices.Select(d => new MqttDeviceViewModel()
        {
            Id = d.Id,
            DeviceType = d.DeviceType,
            CreatedOn = d.CreatedOn,
            IsActive = d.IsActive,
            MqttClientId = d.MqttClientId ?? ""
        }).ToList();

        return View(vm);
    }

    public IActionResult RequestDeviceCreation()
    {
        string code = _serviceManager.MQTTBrokerService.RequestDeviceCreation();
        TempData["MqttCode"] = code;
        return RedirectToAction(nameof(Devices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Publish(string topic, string payload)
    {
        _serviceManager.MQTTBrokerService.Publish(topic, payload);

        return RedirectToAction(nameof(Index));
    }
}
