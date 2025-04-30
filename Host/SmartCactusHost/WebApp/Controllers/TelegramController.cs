using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using WebApp.Models;
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
        TelegramViewModel vm = new()
        {
            IsConnected = _serviceManager.TelegramBotService.IsConnected,
            BotLink = _serviceManager.TelegramBotService.BotLink,
        };
        return View(vm);
    }
    
    public IActionResult StartTelgramBot()
    {
        _serviceManager.TelegramBotService.StartBot();
        return RedirectToAction("Index");
    }
}
