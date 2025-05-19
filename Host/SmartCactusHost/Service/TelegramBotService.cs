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
    private readonly IServiceManager _serviceManager;
    private readonly TelegramBot.TelegramBot _bot;

    public TelegramBotService(ILogger logger, IRepositoryManager repositoryManager, IServiceManager serviceManager, IConfiguration configuration)
    {
        _logger = logger;
        _repositoryManager = repositoryManager;
        _serviceManager = serviceManager;
        _bot = TelegramBot.TelegramBot.InitializeInstance(configuration["telegram:api_key"] ?? "", _logger);

        _bot.LoginUser += LoginUser;
        _bot.SetUserLoginStatus += SetUserLoginStatus;
        _bot.SetUserRole += SetUserRole;
        _bot.GetActiveRegistrationRequests += GetActiveRegistrationRequests;
        _bot.GetAllRegistredUsers += GetAllRegistredUsers;
        _bot.GetUserById += id => _repositoryManager.User.GetUser(id, false);

        _bot.CreateDeviceRequest += _serviceManager.MQTTBrokerService.RequestDeviceCreation;
        _bot.GetRegisteredDevices += GetRegisteredDevices;
        _bot.GetTopicsWithConnectionForDevice += GetTopicsWithConnectionForDevice;

        _bot.GetSubscriptionActions += () => _repositoryManager.TelegramBrokerAction.GetSubscriptionActions(false).ToList();
        _bot.GetPublicationActions += () => _repositoryManager.TelegramBrokerAction.GetPublicationActions(false).ToList();
        _bot.DeleteAction += DeleteAction;
        _bot.CreateBrokerAction += CreateBrokerAction;

        _bot.GetLastTopicMessage += GetLastTopicMessage;
        _bot.PublishMessage += PublishMessage;
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
 
    private void SetUserRole(Guid guid, UserRole role)
    {
        var user = _repositoryManager.User.GetUser(guid, true);
        if (user is not null)
        {
            user.Role = role;
            _repositoryManager.Save();
        }
    }

    private List<User> GetActiveRegistrationRequests()
    {
        return _repositoryManager.User.GetAllUsers(false).Where(u => u.LoginStatus == LoginStatus.Requested).ToList();
    }

    private List<User> GetAllRegistredUsers()
    {
        return _repositoryManager.User.GetAllUsers(false).Where(u => u.LoginStatus != LoginStatus.Requested).ToList();
    }

    private List<Device> GetRegisteredDevices()
    {
        return _repositoryManager.Device
                .GetAllDevices(false)
                .Where(d => !string.IsNullOrEmpty(d.MqttClientId))
                .ToList();
    }

    private (List<Topic>, List<DeviceTopic>) GetTopicsWithConnectionForDevice(Guid guid)
    {
        var connections = _repositoryManager.DeviceTopic
            .GetConnectionsByDevice(guid, false)
            .ToList();
        var topics = _repositoryManager.Topic
            .GetAllTopics(false)
            .Where(t => connections.Any(c => c.TopicId == t.Id))
            .ToList();

        return (topics, connections);
    }

    private void DeleteAction(Guid guid)
    {
        var action = _repositoryManager.TelegramBrokerAction.GetAction(guid, false);
        if (action is not null)
        {
            _repositoryManager.TelegramBrokerAction.DeleteAction(action);
        }
    }

    private bool CreateBrokerAction(TelegramBrokerAction action)
    {
        var topic = _repositoryManager.Topic.GetTopicByName(action.Topic, false);
        if (topic is null)
        {
            return false;
        }

        _repositoryManager.TelegramBrokerAction.CreateAction(action);
        _repositoryManager.Save();
        return true;
    }

    private MqttMessage? GetLastTopicMessage(string topicName)
    {
        var topic = _repositoryManager.Topic.GetTopicByName(topicName, false);
        return _repositoryManager.Message.GetLastMessageInTopic(topic, false);
    }

    private async void PublishMessage(string topic, string payload)
    {
        await _serviceManager.MQTTBrokerService.Publish(topic, payload);
    }

    public bool IsConnected => _bot.IsConnected;

    public string BotLink => _bot.BotLink().Result;

    public void StartBot()
    {
        _bot.StartRecieving();
    }
}
