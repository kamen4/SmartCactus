using Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository.Contracts;
using Service.Contracts;
using WebApp.Models;
using ILogger = LoggerService.ILogger;

namespace WebApp.Controllers;

public class TelegramController : Controller
{
    private readonly ILogger _logger;
    private readonly IServiceManager _serviceManager;
    private readonly IRepositoryManager _repositoryManager;
    public TelegramController(ILogger logger, IServiceManager serviceManager, IRepositoryManager repositoryManager)
    {
        _logger = logger;
        _serviceManager = serviceManager;
        _repositoryManager = repositoryManager;
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
    
    public IActionResult Users()
    {
        var users = _repositoryManager.User.GetAllUsers(false);
        var vm = users.Select(u => new TelegramUserViewModel()
        {
            Id = u.Id,
            LoginStatus = u.LoginStatus,
            Role = u.Role,
            TelegramId = u.TelegramId,
            TelegramUsername = u.TelegramUsername,
            FirstName = u.FirstName,
            LastName = u.LastName
        }).ToList();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleLoginStatus(Guid id, LoginStatus newStatus)
    {   
        var user = _repositoryManager.User.GetUser(id, trackChanges: true); ;
        if (user is null)
        {
            return NotFound();
        }

        user.LoginStatus = newStatus;
        _repositoryManager.Save();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUserRole(Guid id, UserRole newRole)
    {
        var user = _repositoryManager.User.GetUser(id, trackChanges: true); ;
        if (user is null)
        {
            return NotFound();
        }

        user.Role = newRole;
        _repositoryManager.Save();

        return RedirectToAction(nameof(Users));
    }

    public IActionResult StartTelgramBot()
    {
        _serviceManager.TelegramBotService.StartBot();
        return RedirectToAction(nameof(Index));
    }
}
