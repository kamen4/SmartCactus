using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Paging;

public class Button
{
    private static int ID_CNTR = 0;
    public int Id { get; private set; }

    private static readonly Dictionary<int, Button> _pool = [];
    public static Button? GetButton(int id)
    {
        return _pool.TryGetValue(id, out var btn) ? btn : null;
    }
    public static bool TryGetButton(int id, out Button? btn)
    {
        btn = GetButton(id);
        return btn is not null;
    }

    public string? Text { get; set; }
    public Page? ParentPage { get; set; }
    public Action? Handler { get; set; }
    public string? CallbackMessage { get; set; }

    public Button(string text, string? callbackMessage = null, Action? handler = null)
    {
        Id = ID_CNTR++;
        _pool.Add(Id, this);

        Text = text;
        CallbackMessage = callbackMessage;
        Handler = handler;
    }

    public InlineKeyboardButton GetTelegramButton()
    {
        return InlineKeyboardButton.WithCallbackData(Text ?? Id.ToString(), CallbackMessage ?? Id.ToString());
    }
}