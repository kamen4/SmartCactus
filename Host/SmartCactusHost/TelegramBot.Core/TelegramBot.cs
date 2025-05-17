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
                        if (msg == "/start")
                        {
                            await SendPage(Page.GetPage("main"), chatId);
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
        }

        await UpdatePage(page, chatId, msgId);
    }

    private async Task SendPage(Page page, long chatId)
    {
        await _botClient.SendMessage(chatId, page.Text ?? "", ParseMode.MarkdownV2, replyMarkup: page.GetTelegramKeyboard());
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
