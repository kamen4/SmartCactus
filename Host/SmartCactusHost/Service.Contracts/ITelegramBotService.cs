namespace Service.Contracts;

public interface ITelegramBotService
{
    bool IsConnected { get; }
    string BotLink { get; }
    void StartBot();
}
