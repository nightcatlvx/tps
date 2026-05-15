using NLog;

namespace Common.Logging;

/// <summary>
/// NLog 日志帮助类
/// </summary>
public static class LogHelper
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public static void Trace(string message) => _logger.Trace(message);
    public static void Debug(string message) => _logger.Debug(message);
    public static void Info(string message) => _logger.Info(message);
    public static void Warn(string message) => _logger.Warn(message);
    public static void Error(string message) => _logger.Error(message);
    public static void Error(Exception ex, string message) => _logger.Error(ex, message);
    public static void Fatal(string message) => _logger.Fatal(message);

    public static void Trace(string message, params object[] args) => _logger.Trace(message, args);
    public static void Debug(string message, params object[] args) => _logger.Debug(message, args);
    public static void Info(string message, params object[] args) => _logger.Info(message, args);
    public static void Warn(string message, params object[] args) => _logger.Warn(message, args);
    public static void Error(string message, params object[] args) => _logger.Error(message, args);
}
