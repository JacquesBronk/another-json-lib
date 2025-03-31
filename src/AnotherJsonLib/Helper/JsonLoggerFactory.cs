using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnotherJsonLib.Helper;

/// <summary>
/// Provides logging services for AnotherJsonLib using native .NET logging.
/// Loads configuration from appsettings.json and allows overrides via code.
/// Logging is thread-safe and respects log levels for each category.
/// </summary>
public class JsonLoggerFactory : IDisposable
{
    private ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Determines whether logging is enabled.
    /// When false, GetLogger calls return a no-op logger.
    /// </summary>
    private bool IsLoggingEnabled { get; set; } = true;
    
    /// <summary>
    /// The default log level to use if not specified in configuration.
    /// This affects all loggers created by this factory unless overridden by configuration.
    /// </summary>
    public LogLevel MinimumLogLevel { get; private set; } = LogLevel.Debug;
    
    /// <summary>
    /// Gets the currently effective minimum log level, taking into account both
    /// the default setting and any configuration that may override it.
    /// </summary>
    public LogLevel EffectiveMinimumLogLevel 
    { 
        get
        {
            if (!IsLoggingEnabled)
                return LogLevel.None;
                
            var configuredLevel = GetConfiguredLogLevel();
            return configuredLevel ?? MinimumLogLevel;
        }
    }

    private static readonly Lazy<JsonLoggerFactory> instance = new Lazy<JsonLoggerFactory>(() => new JsonLoggerFactory());

    /// <summary>
    /// The singleton instance of the LoggerFactory.
    /// </summary>
    public static JsonLoggerFactory Instance => instance.Value;

    private JsonLoggerFactory()
    {
        _configuration = LoadConfiguration();
        _loggerFactory = CreateLoggerFactory();
    }

    private IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Creates a logger factory that loads configuration from appsettings.json (if available)
    /// and adds a Console logger.
    /// </summary>
    private ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            // First apply any configuration from appsettings.json
            builder.AddConfiguration(_configuration.GetSection("Logging"));
            
            // If no minimum level was specified in configuration, apply our default
            if (GetConfiguredLogLevel() == null)
            {
                builder.SetMinimumLevel(MinimumLogLevel);
            }
            
            builder.AddConsole();
        });
    }
    
    /// <summary>
    /// Retrieves the configured log level from appsettings.json, if any.
    /// </summary>
    private LogLevel? GetConfiguredLogLevel()
    {
        var defaultLevelStr = _configuration.GetSection("Logging:LogLevel:Default").Value;
        if (string.IsNullOrEmpty(defaultLevelStr))
            return null;
            
        if (Enum.TryParse<LogLevel>(defaultLevelStr, true, out var level))
            return level;
            
        return null;
    }

    /// <summary>
    /// Allows a developer to override or extend the logging configuration.
    /// The provided configuration action will be applied on top of the default settings.
    /// </summary>
    /// <param name="configure">An action to configure the ILoggingBuilder.</param>
    public void Configure(Action<ILoggingBuilder> configure)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            // Apply base configuration
            builder.AddConfiguration(_configuration.GetSection("Logging"));
            
            // If no minimum level was specified in configuration, apply our default
            if (GetConfiguredLogLevel() == null)
            {
                builder.SetMinimumLevel(MinimumLogLevel);
            }
            
            // Apply custom configuration (which might override the above)
            configure(builder);
        });
    }

    /// <summary>
    /// Sets the minimum log level for all loggers.
    /// This will override the level in appsettings.json unless a subsequent Configure call changes it.
    /// </summary>
    /// <param name="level">The minimum log level to use.</param>
    public void SetMinimumLogLevel(LogLevel level)
    {
        MinimumLogLevel = level;
        
        // Rebuild the logger factory to apply the new level
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            // We explicitly set the minimum level first so it takes precedence
            builder.SetMinimumLevel(level);
            
            // Then apply any category-specific filters from configuration
            builder.AddConfiguration(_configuration.GetSection("Logging"));
            
            builder.AddConsole();
        });
    }

    /// <summary>
    /// Retrieves an ILogger for the specified category type.
    /// </summary>
    /// <typeparam name="T">The category type.</typeparam>
    /// <returns>An ILogger instance with the appropriate log level settings.</returns>
    public ILogger GetLogger<T>()
    {
        if (!IsLoggingEnabled)
            return NullLogger.Instance;

        return _loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Retrieves an ILogger for the specified category name.
    /// </summary>
    /// <param name="categoryName">The log category name.</param>
    /// <returns>An ILogger instance with the appropriate log level settings.</returns>
    public ILogger GetLogger(string categoryName)
    {
        if (!IsLoggingEnabled)
            return NullLogger.Instance;

        return _loggerFactory.CreateLogger(categoryName);
    }
    
    /// <summary>
    /// Creates a performance tracker that respects the current logging configuration.
    /// </summary>
    /// <param name="logger">The logger to use for tracking.</param>
    /// <param name="operationName">A descriptive name for the operation being tracked.</param>
    /// <returns>A PerformanceTracker that will log at the appropriate level.</returns>
    public PerformanceTracker CreatePerformanceTracker(ILogger logger, string operationName)
    {
        // Use the configured effective minimum log level
        return new PerformanceTracker(logger, operationName, EffectiveMinimumLogLevel);
    }
    
    /// <summary>
    /// Creates a performance tracker for a specific type that respects the current logging configuration.
    /// </summary>
    /// <typeparam name="T">The type to use as the logger category.</typeparam>
    /// <param name="operationName">A descriptive name for the operation being tracked.</param>
    /// <returns>A PerformanceTracker that will log at the appropriate level.</returns>
    public PerformanceTracker CreatePerformanceTracker<T>(string operationName)
    {
        var logger = GetLogger<T>();
        return CreatePerformanceTracker(logger, operationName);
    }

    /// <summary>
    /// Disposes the underlying logger factory.
    /// </summary>
    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}