using KB = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;
using BtnMatr = System.Collections.Generic.List<System.Collections.Generic.List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>;

namespace TelegramBot;

public static class Configurator
{
    public static class Callback
    {
        public const string RegistrationRequest = "RegistrationRequest";
        public static string AcceptRegistration(string userId) => $"RegistrationRequest/Accept/{userId}";
        public static string DeclineRegistration(string userId) => $"RegistrationRequest/Decline/{userId}";
    }

    public static class InlineKeyboards
    {
        public static KB RegistrationRequest(string userId) => new()
        {
            InlineKeyboard = new BtnMatr() 
            { 
                new() 
                {
                    new("Accept", Callback.AcceptRegistration(userId)),
                    new("Decline", Callback.DeclineRegistration(userId)),
                }
            }
        };
    }
}
