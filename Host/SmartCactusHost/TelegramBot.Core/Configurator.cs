using KB = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;
using BtnMatr = System.Collections.Generic.List<System.Collections.Generic.List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>;
using TelegramBot.Paging;

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
    public static void InitializePages()
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
