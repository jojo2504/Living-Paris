using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using System;
using System.IO;
using static LivingParisApp.Services.EnvironmentSetup.Constants;

namespace LivingParisApp.Services {
    public static class Logger {
        private static ILogger _logger;

        public static void ConfigureLogger() {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .Enrich.FromLogContext()
                .CreateLogger();

            Log("Logging initialized");
        }

        public static void LogMessage(string message, LogEventLevel level = LogEventLevel.Information, Exception ex = null) {
            if (_logger == null) {
                ConfigureLogger(); // Auto-initialize if not done
            }

            try {
                if (ex != null) {
                    _logger.Write(level, ex, message);
                }
                else {
                    _logger.Write(level, message);
                }

                if (level == LogEventLevel.Fatal) {
                    Environment.Exit(1);
                }
            }
            catch (Exception loggingEx) {
                // Fallback to console in case of logging failure
                Console.WriteLine($"Logging failed: {loggingEx.Message}");
                Console.WriteLine($"Original message: [{level}] {message}");
                if (ex != null) {
                    Console.WriteLine($"Original exception: {ex}");
                }
            }
        }

        // Convenience methods
        public static void Log(string message) => LogMessage(message);
        public static void Success(string message) => LogMessage($"SUCCESS: {message}");
        public static void Warning(string message) => LogMessage(message, LogEventLevel.Warning);
        public static void Error(string message = "", Exception ex = null) => LogMessage(message, LogEventLevel.Error, ex);
        public static void Fatal(string message = "", Exception ex = null) => LogMessage(message, LogEventLevel.Fatal, ex);

        public static void ClearLog() {
            try {
                if (File.Exists(LogFilePath)) {
                    File.WriteAllText(LogFilePath, $"Log file cleared at {DateTime.Now}\n");
                    Log("Log file cleared");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to clear log file: {ex.Message}");
            }
        }
    }
}