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
    
    public Func<Entities.Models.User, (LoginStatus status, UserRole role)>? LoginUser { get; set; }
    public Action<Guid, LoginStatus>? SetUserLoginStatus { get; set; }

    private TelegramBot(string API_KEY)
    {
        InitializePages();
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
            Telegram.Bot.Types.User? user = null;
            long chatId = 0;
            string? msg = null;

            if (update.Type == UpdateType.Message)
            {
                user = update.Message?.From;
                chatId = update.Message?.Chat.Id ?? 0;
                msg = update.Message?.Text;
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                user = update.CallbackQuery?.From;
                chatId = update.CallbackQuery?.Message?.Chat.Id ?? 0;
                msg = update.CallbackQuery?.Data;
            }
            else
            {
                _logger?.Warn($"Telegram|Unhandeled update type {update.Type}");
                return;
            }

            if (user is null)
            {
                _logger?.Warn($"Telegram|User is null for update with id {update.Id}");
                return;
            }

            if (LoginUser is null)
            {
                _logger?.Error("Telegram|LoginUser function is not set.");
                return;
            }

            var loginResult = LoginUser(new Entities.Models.User()
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
                await _botClient.SendMessage(chatId, badRegistrationMessages[loginResult.status], cancellationToken: cancellationToken);
                _logger?.Info($"Telegram|Unsuccessful registration for user with id: {user.Id}. Update Id: {update.Id}. Login Result: {loginResult}");
                return;
            }

            //if (update.Type == UpdateType.CallbackQuery)
            //{
            //    await HandleCallbackQuery();
            //    return;
            //}

            //if (update.Type == UpdateType.Message)
            //{
            //    await HandleMessage();
            //    return;
            //}

            switch (update.Type)
            {
                case UpdateType.Message:
                {

                    await HandlePage(botClient, update, "main");
                    return;
                }
                case UpdateType.CallbackQuery:
                {
                    if (msg is null)
                    {
                        _logger?.Error($"Telegram|CallbackQuery message is null for update with id {update.Id}");
                        return;
                    }
                    if (msg.StartsWith(Configurator.Callback.RegistrationRequest) && loginResult.role == UserRole.Admin)
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
                        await HandlePage(botClient, update, msg[5..]);
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

    private async Task HandlePage(ITelegramBotClient botClient, Update update, string pageName)
    {
        long chatId = 0;
        if (update.Type == UpdateType.Message)
        {
            chatId = update.Message?.Chat.Id ?? 0;
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            chatId = update.CallbackQuery?.Message?.Chat.Id ?? 0;
        }
        await botClient.SendMessage(
            chatId,
            Page.GetPage(pageName)?.Text ?? "",
            parseMode: ParseMode.Markdown,
            replyMarkup: Page.GetPage(pageName)?.GetTelegramKeyboard());
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

    private void InitializePages()
    {
        const string mainPage = "main";
        const string settingsPage = "settings";
        const string deviceManagmentPage = "device_managment";
        const string myDevicesPage = "my_devices";
        const string mqttManagmentPage = "mqtt_managment";
        const string subscriptionsPage = "subscriptions";
        const string publicationsPage = "publications";

        List<Page> _ =
        [
            new(mainPage)
            {
                ParrentName = null,
                Text = "🌵 *MAIN* 🌵",
                Buttons =
                [
                    [ new("HELLO"), new("WORLD") ],
                    [ new("Settings", $"page/{settingsPage}") ],
                ]
            },
            new(settingsPage)
            {
                ParrentName = mainPage,
                Text = "⚙️ *SETTINGS* ⚙️",
                Buttons =
                [
                    [ new("Device managment", $"page/{deviceManagmentPage}") ],
                    [ new("MQTT managment", $"page/{mqttManagmentPage}") ],
                ]
            },
            new(deviceManagmentPage)
            {
                ParrentName = settingsPage,
                Text = "📱 *Device managment* 📱",
                Buttons =
                [
                    [ new("Register new device") ],
                    [ new("View my devices") ],
                ]
            },
            new(myDevicesPage)
            {
                ParrentName = deviceManagmentPage,
                Text = "📱 *My devices* 📱",
                Buttons =
                [
                    [ new("esp01 dht22"), new("esp01 rele") ],
                    [ new("nodemcu light") ],
                ]
            },
            new(mqttManagmentPage)
            {
                ParrentName = settingsPage,
                Text = "🕸 *MQTT managment* 🕸",
                Buttons =
                [
                    [ new("Subscriptions", $"page/{subscriptionsPage}") ],
                    [ new("Publications", $"page/{publicationsPage}") ],
                ]
            },
            new(subscriptionsPage)
            {
                ParrentName = settingsPage,
                Text = "⬇️ *Subscriptions* ⬇️",
                Buttons =
                [
                    [ new("Available topics") ],
                    [ new("My topics") ],
                ]
            },
            new(publicationsPage)
            {
                ParrentName = settingsPage,
                Text = "⬆️ *Publications* ⬆\nTap to remove or edit",
                Buttons =
                [
                    [ new("led1"), new("kettle"), new("room1") ],
                    [ new("Add NEW") ],
                ]
            },
        ];
    }
}
