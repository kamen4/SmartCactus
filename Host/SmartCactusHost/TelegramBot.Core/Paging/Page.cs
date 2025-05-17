using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Paging;

public class Page
{
    private static readonly Dictionary<string, Page> _pool = [];

    public string Name { get; private set; }
    public string? ParrentName { get; set; }
    public string? Text { get; set; }
    public List<List<Button>>? Buttons { get; set; }

    private Page() {}

    public Page(string name)
    {
        Name = name;
        _pool.Add(name, this);
    }

    public static Page? GetPage(string name)
    {
        return _pool.TryGetValue(name, out var page) ? page : null;
    }
    public static bool TryGetPage(string name, out Page? page)
    {
        page = GetPage(name);
        return page is not null;
    }

    public Page GetCopy()
    {
        var btnsCopy = Buttons?.Select(x => x.Select(x => x).ToList()).ToList();
        var copy = new Page()
        {
            Name = Name,
            Text = Text,
            ParrentName = ParrentName,
            Buttons = btnsCopy
        };
        return copy;
    }

    public InlineKeyboardMarkup GetTelegramKeyboard()
    {
        if (Buttons == null)
        {
            throw new NullReferenceException("Keyboard must have at least 1 button");
        }

        List<List<InlineKeyboardButton>> telegramButtons = [];
        foreach (var row in Buttons)
        {
            List<InlineKeyboardButton> telegramRow = [];
            foreach (var btn in row)
            {
                telegramRow.Add(btn.GetTelegramButton());
            }
            telegramButtons.Add(telegramRow);
        }
        if (ParrentName != null)
        {
            telegramButtons.Add([new Button("Back", $"page/{ParrentName}").GetTelegramButton()]);
        }

        return new InlineKeyboardMarkup(telegramButtons);
    }
}
