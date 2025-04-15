using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using ILogger = LoggerService.ILogger;

namespace WebApp.Controllers;

public class TelegramController : Controller
{
    private readonly ILogger _logger;
    private readonly IServiceManager _serviceManager;

    public TelegramController(ILogger logger, IServiceManager serviceManager)
    {
        _logger = logger;
        _serviceManager = serviceManager;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult StartTelgramBot()
    {
        _serviceManager.TelegramBotService.StartBot();
        TempData["Message"] = "Bot started!";
        return RedirectToAction("Index");
    }
}
