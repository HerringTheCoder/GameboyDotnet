using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace GameboyDotnet.SDL;

public static class LoggerHelper
{
  public static ILogger<T> GetLogger<T>(LogLevel minimumLogLevel) where T : class
  {
    var loggerFactory = LoggerFactory.Create(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(minimumLogLevel);
    });
    
    var logger = loggerFactory.CreateLogger<T>();
    
    logger.LogInformation("Information log test");
    logger.LogWarning("Warning log test");
    logger.LogError("Error log test");
    logger.LogDebug("Debug log test");
    
    if (loggerFactory is IDisposable disposableLoggerFactory)
    {
      disposableLoggerFactory.Dispose();
    }

    return logger;
  }
}
