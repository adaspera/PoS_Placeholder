namespace PoS_Placeholder.Server.Logging;

public class Logger : ILogger
{
    private readonly LoggerProvider _loggerProvider;

    public Logger(LoggerProvider loggerProvider)
    {
        _loggerProvider = loggerProvider;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        
        Thread thread = new Thread(new ThreadStart( () =>
        {
            string logMessage = formatter(state, exception);
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] - {logMessage}: {exception?.Message}";
            
            try
            {
                Monitor.Enter(_loggerProvider.Writer);
                _loggerProvider.Writer.WriteLine(logEntry);
                _loggerProvider.Writer.Flush();
            }
            finally
            {
                Monitor.Exit(_loggerProvider.Writer);
            }
        }));
        thread.Start();
    }
}