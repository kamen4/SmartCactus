using Telegram.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using LoggerService;
using TelegramBot.Paging;
using Entities.Enums;
using Entities.Models;
using Telegram.Bot.Types.ReplyMarkups;
using User = Entities.Models.User;
using TGUser = Telegram.Bot.Types.User;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace TelegramBot;

public class TelegramBot
{
    private static TelegramBot? _instance;
    private readonly ReceiverOptions _receiverOptions;
    private readonly TelegramBotClient _botClient;
    private static ILogger? _logger;

    private bool _botStarted = false;
    public bool IsConnected => _botStarted;

    public async Task<string> BotLink()
    {
        var botUser = await _botClient.GetMe();
        return $"https://t.me/{botUser.Username}";
    }

    #region outer functions
    public Func<User, (LoginStatus status, UserRole role)>? LoginUser { get; set; }
    public Action<Guid, LoginStatus>? SetUserLoginStatus { get; set; }
    public Action<Guid, UserRole>? SetUserRole { get; set; }
    public Func<List<User>>? GetActiveRegistrationRequests { get; set; }
    public Func<List<User>>? GetAllRegistredUsers { get; set; }
    public Func<Guid, User?>? GetUserById { get; set; }

    public Func<string>? CreateDeviceRequest { get; set; }
    public Func<List<Device>>? GetRegisteredDevices { get; set; }
    public Func<Guid, (List<Topic>, List<DeviceTopic>)>? GetTopicsWithConnectionForDevice { get; set; }

    public Func<List<TelegramBrokerAction>>? GetSubscriptionActions { get; set; }
    public Func<List<TelegramBrokerAction>>? GetPublicationActions { get; set; }
    public Action<Guid>? DeleteAction { get; set; }
    public Func<TelegramBrokerAction, bool>? CreateBrokerAction { get; set; }

    public Func<string, MqttMessage?>? GetLastTopicMessage { get; set; }
    public Action<string, string>? PublishMessage { get; set; }
    #endregion

    private TelegramBot(string API_KEY)
    {
        Configurator.Paging.InitializePages();
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates =
            [
                UpdateType.Message,
                UpdateType.CallbackQuery,
            ],
            DropPendingUpdates = true,
        };
        _botClient = new(API_KEY);
        _logger?.Info($"Telegram|Bot initialized with id: {_botClient.BotId}.");
    }

    public static TelegramBot InitializeInstance(string API_KEY, ILogger? logger = null)
    {
        _logger ??= logger;
        _instance ??= new(API_KEY);
        return _instance;
    }

    public void StartRecieving()
    {
        if (!_botStarted)
        {
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions);
            _botStarted = true;
            _logger?.Info("Telegram|Bot started receiving messages.");
        }
        else
        {
            _logger?.Warn("Telegram|Trying to start Bot, that alredy started receiving messages.");
        }
    }

    public void SendMessage(long chatId, string message, ReplyMarkup? replyKeyboard = null)
    {
        _botClient.SendMessage(chatId, message, replyMarkup: replyKeyboard);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var (user, chatId, msg, msgId) = GetUpdateInfo(update);

            var userRole = await EnsureUserLogined(user, chatId);

            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        if (msg.StartsWith('/'))
                        {
                            await HandleCommand(chatId, msg, msgId);
                        }
                        return;
                    }
                case UpdateType.CallbackQuery:
                    {
                        if (msg.StartsWith(Configurator.Callback.RegistrationRequest) && userRole == UserRole.Admin)
                        {
                            var parts = msg.Split('/');
                            SetUserLoginStatus(new Guid(parts[2]), parts[1] == "Accept" ? LoginStatus.Accepted : LoginStatus.Blocked);
                            await _botClient.EditMessageReplyMarkup(
                                chatId,
                                update.CallbackQuery?.Message?.MessageId ?? throw new NullReferenceException(),
                                new InlineKeyboardMarkup()
                                {
                                    InlineKeyboard = [[ InlineKeyboardButton.WithCallbackData($"{parts[1].TrimEnd('e')}ed") ]]
                                },
                                cancellationToken: cancellationToken);
                        }
                        else if (msg.StartsWith("page/"))
                        {
                            await HandlePage(chatId, msg, msgId);
                        }
                        else if (int.TryParse(msg, out var id) && Button.TryGetButton(id, out var btn))
                        {
                            btn?.Handler?.Invoke();
                        }
                        else
                        {
                            _logger?.Warn($"Telegram|Unhandeled callback query: {msg}");
                        }
                        return;
                    }
            }
        }
        catch (Exception e)
        {
            _logger?.Error($"Telegram|{e.Message}");
        }
    }

    private async Task HandleCommand(long chatId, string msg, int msgId)
    {
        if (msg == "/start")
        {
            await HandlePage(chatId, $"page/{Configurator.Paging.main}", 0);
            return;
        }
        if (msg.StartsWith("/add-sub"))
        {
            var splittedMsg = msg.Split('=');
            TelegramBrokerAction subAction = new()
            {
                Id = Guid.NewGuid(),
                EventType = EventType.Subscription,
                Topic = splittedMsg[1],
                Selector = splittedMsg[2],
                Name = splittedMsg[3]
            };
            if (CreateBrokerAction(subAction))
            {
                await HandlePage(chatId, $"page/{Configurator.Paging.subscriptions}", 0);
            }
            else
            {
                SendMessage(chatId, "Something went wrong while creating subscription :(\nTry again!");
                await HandlePage(chatId, $"page/{Configurator.Paging.add_subscription}", 0);
            }
            return;
        }
        if (msg.StartsWith("/add-pub"))
        {
            var splittedMsg = msg.Split('=');
            TelegramBrokerAction subAction = new()
            {
                Id = Guid.NewGuid(),
                EventType = EventType.Publication,
                Topic = splittedMsg[1],
                Payload = splittedMsg[2],
                Name = splittedMsg[3]
            };
            if (CreateBrokerAction(subAction))
            {
                await HandlePage(chatId, $"page/{Configurator.Paging.publications}", 0);
            }
            else
            {
                SendMessage(chatId, "Something went wrong while creating publication :(\nTry again!");
                await HandlePage(chatId, $"page/{Configurator.Paging.add_publication}", 0);
            }
            return;
        }
    }

    private (TGUser user, long chatId, string msg, int msgId) GetUpdateInfo(Update update)
    {
        TGUser? user = null;
        long chatId = 0;
        string? msg = null;
        int msgId = 0;

        if (update.Type == UpdateType.Message)
        {
            user = update.Message?.From;
            chatId = update.Message?.Chat.Id ?? 0;
            msg = update.Message?.Text;
            msgId = update.Message?.Id ?? 0;
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            user = update.CallbackQuery?.From;
            chatId = update.CallbackQuery?.Message?.Chat.Id ?? 0;
            msg = update.CallbackQuery?.Data;
            msgId = update.CallbackQuery?.Message?.Id ?? 0;
        }
        else
        {
            throw new Exception($"Unhandeled update type {update.Type}");
        }

        if (user is null)
        {
            throw new Exception($"User is null for update with id {update.Id}");
        }

        if (string.IsNullOrEmpty(msg))
        {
            throw new Exception($"Message is null for update with id {update.Id}");
        }

        return (user, chatId, msg, msgId);
    }

    private async Task<UserRole> EnsureUserLogined(TGUser user, long chatId)
    {
        if (LoginUser is null)
        {
            throw new Exception("LoginUser function is not set.");
        }

        var loginResult = LoginUser(new User()
        {
            TelegramId = user.Id,
            TelegramChatId = chatId,
            TelegramUsername = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
        });

        Dictionary<LoginStatus, string> badRegistrationMessages = new()
        {
            [LoginStatus.Blocked] = "You have been blocked. Contact admin for more info.",
            [LoginStatus.Requested] = "Registartation requested, wait for admin response.",
        };
        if (loginResult.status != LoginStatus.Accepted)
        {
            await _botClient.SendMessage(chatId, badRegistrationMessages[loginResult.status]);
            throw new Exception($"Unsuccessful registration for user with id: {user.Id}. Login Result: {loginResult}");
        }

        return loginResult.role;
    }

    private async Task HandlePage(long chatId, string msg, int msgId)
    {
        var pageSplit = msg.Split('/');
        var pageName = pageSplit[1];
        var page = Page.GetPage(pageName);
        if (page is null)
        {
            _logger?.Warn($"Telegram|Page with name {pageName} not found.");
            return;
        }

        switch (pageName)
        {
            case Configurator.Paging.main:
                {
                    page = page.GetCopy();
                    List<TelegramBrokerAction> subs = GetSubscriptionActions();
                    page.Text = $"{page.Text}\n{
                        string.Join('\n', subs.Select(a =>
                        {
                            MqttMessage? msg = GetLastTopicMessage(a.Topic);
                            return $"_{a.Name}_ \\- {
                                Utils.JsonUtils.GetJsonDataWithSelector(msg?.Payload ?? "", a.Selector ?? "")}";
                        })).Replace(".", "\\.")}";

                    List<TelegramBrokerAction> pubs = GetPublicationActions().OrderBy(x => x.Topic).ToList();
                    page.Buttons ??= [];
                    page.Buttons.InsertRange(0, pubs.Select(a => new List<Button>() 
                    {
                        new($"{a.Name}", handler: () => {
                            PublishMessage(a.Topic, a.Payload);
                        })  
                    }.ToList()));
                    break;
                }

            case Configurator.Paging.active_requests:
                {
                    page = page.GetCopy();
                    List<User> requests = GetActiveRegistrationRequests();
                    page.Text = $"{page.Text}\nCount: {requests.Count}\n**Tap to view:**";
                    page.Buttons = requests.Select(user => new List<Button>()
                    { new(
                        $"@{user.TelegramUsername}",
                        $"page/{Configurator.Paging.request_info}/{user.Id}"
                    )}).ToList();
                    break;
                }
            case Configurator.Paging.all_users:
                {
                    page = page.GetCopy();
                    List<User> users = GetAllRegistredUsers();
                    page.Text = $"{page.Text}\nCount: {users.Count}\n**Tap to view:**";
                    page.Buttons = users.Select(user => new List<Button>()
                    { new(
                        $"{(user.LoginStatus == LoginStatus.Accepted ? "✅" : "🛑")} @{user.TelegramUsername}",
                        $"page/{Configurator.Paging.user_info}/{user.Id}"
                    )}).ToList();
                    break;
                }
            case Configurator.Paging.request_info:
                {
                    page = page.GetCopy();
                    User user = GetUserById(Guid.Parse(pageSplit[2]));
                    page.Text = $"{page.Text}\nRequest from user: @{user.TelegramUsername}";
                    page.Buttons =
                    [
                        [ new("Approve", handler: async () =>
                        {
                            SetUserLoginStatus(user.Id, LoginStatus.Accepted);
                            var page = Page.GetPage(Configurator.Paging.user_managment);
                            if (page is null) { return; }
                            await UpdatePage(page, chatId, msgId);
                        }) ],
                        [ new("Reject", handler: async () =>
                        {
                            SetUserLoginStatus(user.Id, LoginStatus.Blocked);
                            await HandlePage(chatId, $"page/{Configurator.Paging.user_managment}", msgId);
                        }) ],
                    ];
                    break;
                }
            case Configurator.Paging.user_info:
                {
                    page = page.GetCopy();
                    User user = GetUserById(Guid.Parse(pageSplit[2]));
                    page.Text = $@"{page.Text}
_User_ : @{user.TelegramUsername.Replace("_", "\\_")}
_Guid_ : `{user.Id}`
_Login Status_ : {user.LoginStatus}
_Role_ : {(user.Role.HasFlag(UserRole.Admin) ? "Admin" : "User")}";
                    page.Buttons =
                    [
                        [ new((user.LoginStatus == LoginStatus.Blocked ? "Approve" : "Block"), handler: async () =>
                        {
                            SetUserLoginStatus(user.Id, (user.LoginStatus == LoginStatus.Blocked ? LoginStatus.Accepted : LoginStatus.Blocked));
                            await HandlePage(chatId, $"page/{Configurator.Paging.user_info}/{user.Id}", msgId);
                        }) ],
                        [ new((user.Role.HasFlag(UserRole.Admin) ? "Remove Admin" : "Make Admin"), handler: async () =>
                        {
                            SetUserRole(user.Id, (user.Role.HasFlag(UserRole.Admin) ? UserRole.User : UserRole.Admin));
                            await HandlePage(chatId, $"page/{Configurator.Paging.user_info}/{user.Id}", msgId);
                        }) ],
                    ];

                    break;
                }

            case Configurator.Paging.device_request:
                {
                    page = page.GetCopy();
                    string token = CreateDeviceRequest();
                    page.Text = $"{page.Text}\nDevice request created successfully\\!\n*Your Token*:\n`{token}`";
                    break;
                }
            case Configurator.Paging.devices:
                {
                    page = page.GetCopy();
                    List<Device> devices = GetRegisteredDevices();
                    if (pageSplit.Length == 2)
                    {
                        page.Text = $"{page.Text}\nRegistered devices count: {devices.Count}\nSelect device id to view info:";
                    }
                    else
                    {
                        var selectedDevice = devices.FirstOrDefault(x => x.Id.Equals(Guid.Parse(pageSplit[2])));
                        if (selectedDevice is null) return; //TODO ERROR
                        (List<Topic> topics, List<DeviceTopic> connections) = GetTopicsWithConnectionForDevice(selectedDevice.Id);
                        page.Text = @$"{page.Text}
Selected device:
_Guid_ : `{selectedDevice.Id}`
_Mqtt Id_ : {selectedDevice.MqttClientId.Replace("-", "\\-")}
_Device Type_ : {(selectedDevice.DeviceType.HasFlag(DeviceType.Sensor) ? "Sensor " : "")}{(selectedDevice.DeviceType.HasFlag(DeviceType.Output) ? "Output" : "")}
_Created On_ : {(selectedDevice.CreatedOn.ToString("d")).Replace("-", "\\-")}
_Topics_ :
{ string.Join("\n", topics
    .Join(connections, t => t.Id, c => c.TopicId, (t, c) => new { t.Name, c.EventType })
    .Select(x => $"{x.Name} \\- {x.EventType}")) }

Registered devices count: {devices.Count}
Select device id to view info:";
                    }
                    page.Buttons = devices.Select(d => new List<Button>() { 
                        new(d.MqttClientId ?? "", $"page/{Configurator.Paging.devices}/{d.Id}") 
                    }).ToList();
                    break;
                }

            case Configurator.Paging.remove_subscription:
                {
                    page = page.GetCopy();
                    List<TelegramBrokerAction> subActions = GetSubscriptionActions();
                    page.Text = $"{page.Text}\nSubscriptions found: {subActions.Count}\nTap to remove:";
                    page.Buttons = subActions.Select(a => new List<Button>()
                    {
                        new(a.Topic, handler: async () =>
                        {
                            DeleteAction(a.Id);
                            await HandlePage(chatId, $"page/{Configurator.Paging.remove_subscription}", msgId);
                        })
                    }).ToList();
                    break;
                }
            case Configurator.Paging.remove_publication:
                {
                    page = page.GetCopy();
                    List<TelegramBrokerAction> pubActions = GetPublicationActions();
                    page.Text = $"{page.Text}\nPublications found: {pubActions.Count}\nTap to remove:";
                    page.Buttons = pubActions.Select(a => new List<Button>()
                    {
                        new(a.Topic, handler: async () =>
                        {
                            DeleteAction(a.Id);
                            await HandlePage(chatId, $"page/{Configurator.Paging.remove_publication}", msgId);
                        })
                    }).ToList();
                    break;
                }
        }

        if (msgId == 0)
        {
            await SendPage(page, chatId);
        }
        else
        {
            await UpdatePage(page, chatId, msgId);
        }
    }

    private async Task<Message> SendPage(Page page, long chatId)
    {
        return await _botClient.SendMessage(chatId, page.Text ?? "", ParseMode.MarkdownV2, replyMarkup: page.GetTelegramKeyboard());
    }

    private async Task UpdatePage(Page page, long chatId, int msgId)
    {
        await UpdateMessage(chatId, msgId, page.Text ?? "", page.GetTelegramKeyboard());
    }

    private async Task UpdateMessage(long chatId, int messageId, string? text, InlineKeyboardMarkup? replyMarkup)
    {
        if (text is not null)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                text,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: replyMarkup);
            return;
        }

        if (replyMarkup is not null)
        {
            await _botClient.EditMessageReplyMarkup(
                chatId,
                messageId,
                replyMarkup);
        }

    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        _logger?.Error($"Telegram|{errorMessage}");

        return Task.CompletedTask;
    }
}
