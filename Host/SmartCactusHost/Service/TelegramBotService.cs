using Entities.Enums;
using Entities.Models;
using LoggerService;
using Microsoft.Extensions.Configuration;
using Repository.Contracts;
using Service.Contracts;
using TelegramBot;

namespace Service;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger _logger;
    private readonly IRepositoryManager _repositoryManager;
    private readonly TelegramBot.TelegramBot _bot;

    public TelegramBotService(ILogger logger, IRepositoryManager repositoryManager, IConfiguration configuration)
    {
        _logger = logger;
        _repositoryManager = repositoryManager;
        _bot = TelegramBot.TelegramBot.InitializeInstance(configuration["telegram:api_key"] ?? "", _logger);
        _bot.LoginUser += LoginUser;
        _bot.SetUserLoginStatus += SetUserLoginStatus;
    }

    private (LoginStatus, UserRole) LoginUser(User user)
    {
        var existingUser = _repositoryManager.User.GetUserByCondition(u => u.TelegramId == user.TelegramId, false);
        if (existingUser is null)
        {
            user.LoginStatus = LoginStatus.Requested;
            _repositoryManager.User.CreateUser(user);
            _repositoryManager.Save();
            SendRequestToAdmin(user);
            return (LoginStatus.Requested, UserRole.User);
        }
        return (existingUser.LoginStatus, existingUser.Role);
    }

    private void SendRequestToAdmin(User user)
    {
        var admin = _repositoryManager.User.GetUserByCondition(u => u.Role == UserRole.Admin, false);
        if (admin is null)
        {
            var firstAdmin = _repositoryManager.User.GetUser(user.Id, true);
            firstAdmin.LoginStatus = LoginStatus.Accepted;
            firstAdmin.Role = UserRole.Admin;
            _repositoryManager.Save();
        }
        else
        {
        _bot.SendMessage(
                admin.TelegramChatId ?? 0,
                $"New registration request from: @{user.TelegramUsername}. \nCheck \"User Managment\" settings.");
        }
    }

    private void SetUserLoginStatus(Guid guid, LoginStatus status)
    {
        var user = _repositoryManager.User.GetUser(guid, true);
        if (user is not null)
        {
            user.LoginStatus = status;
            _repositoryManager.Save();
            _bot.SendMessage(user.TelegramChatId ?? 0, $"Your registration request was {status}");
        }
    }

    public bool IsConnected => _bot.IsConnected;

    public string BotLink => _bot.BotLink().Result;

    public void StartBot()
    {
        _bot.StartRecieving();
    }
}
