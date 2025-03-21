using Microsoft.Extensions.Configuration;
using Telegram.Bot.Polling;
using Telegram.Bot;
using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SCTelegramBot;

internal class Program
{
    private static ITelegramBotClient? _botClient;
    private static ReceiverOptions? _receiverOptions;
    private static string? _apiKey;

    static async Task Main()
    {
        InitVariables();
     
        _botClient = new TelegramBotClient(_apiKey ?? "");
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates =
            [
                UpdateType.Message,
                UpdateType.CallbackQuery,
            ],
            DropPendingUpdates = true,
        };

        using var cts = new CancellationTokenSource();

        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

        var me = await _botClient.GetMe();
        Console.WriteLine($"{me.Username} started!");

        await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                        var message = update.Message;
                        if (message is null || message.Type != MessageType.Text)
                            return;
                        var user = message.From;
                        Console.WriteLine($"{user.Username} ({user.Id}) wrote msg: {message.Text}");

                        var chat = message.Chat;

                        var inlineKeyboard = new InlineKeyboardMarkup(
                                    new List<InlineKeyboardButton[]>()
                                    {
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("button1", "button1"),
                                            InlineKeyboardButton.WithCallbackData("button2", "button2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("button3", "button3"),
                                            InlineKeyboardButton.WithCallbackData("button4", "button4"),
                                            InlineKeyboardButton.WithCallbackData("button5", "button5"),
                                        },
                                    }
                            );

                        await botClient.SendMessage(
                            chat.Id,
                            message.Text, 
                            replyParameters: new ReplyParameters() { MessageId = message.MessageId },
                            replyMarkup: inlineKeyboard);

                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    static void InitVariables()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _apiKey = configuration["TelegramBotApiKey"]?.ToString() ??
            throw new Exception("TelegramBotApiKey not found");
    }
}
