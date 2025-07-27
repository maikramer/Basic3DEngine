using Serilog;

namespace Basic3DEngine.Services;

public static class LoggingService
{
    private static ILogger? _logger;

    public static void Initialize(string logFilePath)
    {
        try
        {
            // Create the logs directory if it doesn't exist
            var directory = Path.GetDirectoryName(logFilePath) ?? "logs";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logFilePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 7,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            LogInfo("Logging service initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize logging service: {ex.Message}");
            // Fallback to console logging if file logging fails
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        }
    }

    public static void LogDebug(string message)
    {
        _logger?.Debug(message);
    }

    public static void LogInfo(string message)
    {
        _logger?.Information(message);
    }

    public static void LogWarning(string message)
    {
        _logger?.Warning(message);
    }

    public static void LogError(string message)
    {
        _logger?.Error(message);
    }

    public static void LogError(Exception ex, string message)
    {
        _logger?.Error(ex, message);
    }

    public static void LogFatal(string message)
    {
        _logger?.Fatal(message);
    }

    public static void LogFatal(Exception ex, string message)
    {
        _logger?.Fatal(ex, message);
    }
}