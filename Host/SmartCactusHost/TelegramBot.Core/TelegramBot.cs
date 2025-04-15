using Telegram.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using TelegramBot.Core.Paging;
using LoggerService;

namespace TelegramBot.Core;

public class TelegramBot
{
    private static TelegramBot? _instance;
    private readonly ReceiverOptions _receiverOptions;
    private readonly TelegramBotClient _botClient;
    private static ILogger? _logger;

    private bool _botStarted = false;

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
        _logger?.Info($"Telegram bot initialized with id: {_botClient.BotId}.");
    }

    public void StartRecieving()
    {
        if (!_botStarted)
        {
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions);
            _botStarted = true;
            _logger?.Info("Telegram bot started receiving messages.");
        }
        else
        {
            _logger?.Warn("Trying to start Telegram bot, that alredy started receiving messages.");
        }
    }
        
    public static TelegramBot InitializeInstance(string API_KEY, ILogger? logger = null)
    {
        _logger ??= logger;
        _instance ??= new(API_KEY);
        return _instance;
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.Info($"New update: {update.Type}");
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        await HandlePage(botClient, update, "main");
                        return;
                    }
                case UpdateType.CallbackQuery:
                    {
                        var query = update.CallbackQuery;
                        if (query is null || query.Data is null)
                        {
                            Console.WriteLine("Query or it's data is empty");
                            return;
                        }

                        if (query.Data.StartsWith("page/"))
                        {
                            await HandlePage(botClient, update, query.Data[5..]);
                        }
                        else if (int.TryParse(query.Data, out var id) && Button.TryGetButton(id, out var btn))
                        {
                            btn?.Handler?.Invoke();
                        }
                        else
                        {
                            Console.WriteLine($"Unhandeled query: {query.Data}");
                        }
                        return;
                    }
            }
        }
        catch (Exception e)
        {
            _logger?.Error(e.Message);
        }
    }

    private static async Task HandlePage(ITelegramBotClient botClient, Update update, string pageName)
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

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        _logger?.Error(errorMessage);

        return Task.CompletedTask;
    }

    private static void InitializePages()
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
