using TelegramBot.Paging;
using BtnMatr = System.Collections.Generic.List<System.Collections.Generic.List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>;
using KB = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;

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

    public static class Paging
    {
        #region Pages Names
        //1
        public const string main = "main";
        //2
        public const string settings = "settings";
        //3
        public const string user_managment = "user_managment";
        public const string device_managment = "device_managment";
        public const string menu_managment = "menu_managment";
        //4
        public const string active_requests = "active_requests";
        public const string all_users = "all_users";

        public const string device_request = "device_request";
        public const string devices = "devices";

        public const string subscriptions = "subscriptions";
        public const string publications = "publications";
        //5
        public const string request_info = "request_info";
        public const string user_info = "user_info";

        public const string add_subscription = "add_subscription";
        public const string remove_subscription = "remove_subscription";

        public const string add_publication = "add_publication";
        public const string remove_publication = "remove_publication";
        #endregion
        public static void InitializePages()
        {
            List<Page> _ =
            [
                //1
                new(main)
            {
                ParrentName = null,
                Text = "🌵 *Main* 🌵",
                Buttons =
                [
                    [ new("Settings", $"page/{settings}") ],
                ]
            },
                //2
                new(settings)
                {
                        ParrentName = main,
                        Text = "⚙️ *Settings* ⚙️",
                        Buttons =
                        [
                            [ new("User Managment", $"page/{user_managment}") ],
                            [ new("Device Managment", $"page/{device_managment}") ],
                            [ new("Menu Managment", $"page/{menu_managment}") ],
                        ]
                },
                //3
                new(user_managment)
                {
                    ParrentName = settings,
                    Text = "🙋‍ *User Managment* 🙋",
                    Buttons =
                    [
                        [ new("Active requests", $"page/{active_requests}") ],
                        [ new("All users", $"page/{all_users}") ],
                    ]
                },
                new(device_managment)
                {
                    ParrentName = settings,
                    Text = "📱 *Device Managment* 📱",
                    Buttons =
                    [
                        [ new("Create device request", $"page/{device_request}") ],
                        [ new("Devices", $"page/{devices}") ],
                    ]
                },
                new(menu_managment)
                {
                    ParrentName = settings,
                    Text = "🗒 *Menu Managment* 🗒",
                    Buttons =
                    [
                        [ new("Subscriptions", $"page/{subscriptions}") ],
                        [ new("Publications", $"page/{publications}") ],
                    ]
                },
                //4
                new(active_requests)
                {
                    ParrentName = user_managment,
                    Text = "🤷 *Active Requests* 🤷",
                    Buttons =
                    []
                },
                new(all_users)
                {
                    ParrentName = user_managment,
                    Text = "👉 *All Users* 👈",
                    Buttons =
                    []
                },
                new(device_request)
                {
                    ParrentName = device_managment,
                    Text = "🎫 *Device Request* 🎫",
                    Buttons = []
                },
                new(devices)
                {
                    ParrentName = device_managment,
                    Text = "🤖 *Devices* 🤖",
                    Buttons =
                    []
                },
                new(subscriptions)
                {
                    ParrentName = menu_managment,
                    Text = "📩 *Subscriptions* 📩",
                    Buttons  =
                    [
                        [ new("Add", $"page/{add_subscription}") ],
                        [ new("Remove", $"page/{remove_subscription}") ],
                    ]
                },
                new(publications)
                {
                    ParrentName = menu_managment,
                    Text = "✍️ *Publications* ✍️",
                    Buttons  =
                    [
                        [ new("Add", $"page/{add_publication}") ],
                        [ new("Remove", $"page/{remove_publication}") ],
                    ]
                },
                //5
                new(request_info)
                {
                    ParrentName = active_requests,
                    Text = "_*Request Info*_",
                    Buttons = []
                },
                new(user_info)
                {
                    ParrentName = all_users,
                    Text = "_*User Info*_",
                    Buttons = []
                },
                new(add_subscription)
                {
                    ParrentName = subscriptions,
                    Text = "_*Add Subscription*_\nType command:\n/add\\-sub\\=topic\\=field selector\\=main name",
                    Buttons = []
                },
                new(remove_subscription)
                {
                    ParrentName = subscriptions,
                    Text = "_*Remove Subscription*_",
                    Buttons = []
                },
                new(add_publication)
                {
                    ParrentName = publications,
                    Text = "_*Add Publication*_\nType command:\n/add\\-pub\\=topic\\=payload json\\=main name",
                    Buttons = []
                },
                new(remove_publication)
                {
                    ParrentName = publications,
                    Text = "_*Remove Publication*_",
                    Buttons = []
                },
            ];
        }
    }
}
