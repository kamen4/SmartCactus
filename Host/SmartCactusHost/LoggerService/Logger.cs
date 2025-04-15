using NLog;

namespace LoggerService;

public class Logger : ILogger
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
    static Logger()
    {
        LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().WriteToConsole();
        });
    }
    public void Debug(string message) => _logger.Debug(message);

    public void Error(string message) => _logger.Error(message);

    public void Info(string message) => _logger.Info(message);

    public void Warn(string message) => _logger.Warn(message);
}
