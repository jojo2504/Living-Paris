using System;
using System.IO;
using System.Text.Json;

namespace LivingParisApp.Services.EnvironmentSetup {
    internal static class ConfigLoader {
        // Database properties
        public static string DatabaseHost { get; }
        public static int DatabasePort { get; }
        public static string DatabaseName { get; }
        public static string DatabaseUser { get; }
        public static string DatabasePassword { get; }

        // Path properties
        public static string BaseAppDataPath { get; }
        public static string LogsPath { get; }
        public static string LogFilePath { get; }

        public static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null) {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any()) {
                directory = directory.Parent;
            }
            return directory;
        }

        static ConfigLoader() {
            try {
                DirectoryInfo solutionRoot = TryGetSolutionDirectoryInfo();
                // Construct the path to appsettings.json in the 'config' directory
                string appSettingsPath = Path.Combine(solutionRoot.FullName, "config", "appsettings.json");

                string jsonText = File.ReadAllText(appSettingsPath);

                var config = JsonDocument.Parse(jsonText).RootElement;

                // Load database configuration
                var dbConfig = config.GetProperty("Database");
                DatabaseHost = dbConfig.GetProperty("Host").GetString()!;
                DatabasePort = dbConfig.GetProperty("Port").GetInt32();
                DatabaseName = dbConfig.GetProperty("Name").GetString()!;
                DatabaseUser = dbConfig.GetProperty("User").GetString()!;
                DatabasePassword = dbConfig.GetProperty("Password").GetString()!;

                // Load paths configuration
                var pathsConfig = config.GetProperty("Paths");

                // Get base directory configuration
                var baseDirectoryConfig = pathsConfig.GetProperty("BaseDirectory");
                if (baseDirectoryConfig.GetProperty("Type").GetString() == "AppData") {
                    string baseDirectoryPath = baseDirectoryConfig.GetProperty("Path").GetString()!;
                    BaseAppDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        baseDirectoryPath
                    );
                }

                // Get logs configuration
                var logsConfig = pathsConfig.GetProperty("Logs");
                if (logsConfig.GetProperty("Type").GetString() == "AppData") {
                    string logsPath = logsConfig.GetProperty("Path").GetString()!;
                    LogsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        logsPath
                    );
                    var logFileName = logsConfig.GetProperty("File").GetProperty("Name").GetString();
                    LogFilePath = Path.Combine(LogsPath, logFileName);
                }

                // Create directories if they don't exist
                Directory.CreateDirectory(BaseAppDataPath);
                Directory.CreateDirectory(LogsPath);
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Failed to initialize configuration", ex);
            }
        }
    }

    
}