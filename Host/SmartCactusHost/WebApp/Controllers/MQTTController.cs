using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
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
        return View();
    }

    public IActionResult StartMQTTBroker()
    {
        _serviceManager.MQTTBrokerService.StartBroker();
        TempData["Message"] = "Broker started!";
        return RedirectToAction("Index");
    }
}
