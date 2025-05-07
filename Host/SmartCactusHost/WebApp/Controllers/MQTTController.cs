using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using WebApp.Models;
using ILogger = LoggerService.ILogger;

namespace WebApp.Controllers;

public class MQTTController : Controller
{
    private readonly ILogger _logger;
    private readonly IServiceManager _serviceManager;

    public MQTTController(ILogger logger, IServiceManager serviceManager)
    {
        _logger = logger;
        _serviceManager = serviceManager;
    }

    public IActionResult Index()
    {
        MqttViewModel vm = new()
        {
            IsStarted = _serviceManager.MQTTBrokerService.IsStarted()
        };

        return View(vm);
    }

    public IActionResult RequestDeviceCreation()
    {
        string code = _serviceManager.MQTTBrokerService.RequestDeviceCreation();
        TempData["MqttCode"] = code;
        return RedirectToAction("Index");
    }

    public IActionResult StartMQTTBroker()
    {
        _serviceManager.MQTTBrokerService.StartBroker();
        return RedirectToAction("Index");
    }
}
