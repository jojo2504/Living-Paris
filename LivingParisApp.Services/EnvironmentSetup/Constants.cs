namespace LivingParisApp.Services.EnvironmentSetup{
    public static class Constants {
        public static string DatabaseHost => ConfigLoader.DatabaseHost;
        public static int DatabasePort => ConfigLoader.DatabasePort;
        public static string DatabaseName => ConfigLoader.DatabaseName;
        public static string DatabaseUser => ConfigLoader.DatabaseUser;
        public static string DatabasePassword => ConfigLoader.DatabasePassword;

        public static string BaseAppDataPath => ConfigLoader.BaseAppDataPath;
        public static string LogsPath => ConfigLoader.LogsPath;
        public static string LogFilePath => ConfigLoader.LogFilePath;

        public static string GetConnectionString() {
            return $"Server={DatabaseHost};Port={DatabasePort};Database={DatabaseName};User={DatabaseUser};Password={DatabasePassword}";
        }

        public static DirectoryInfo GetSolutionDirectoryInfo(string currentPath = null) {
            return ConfigLoader.TryGetSolutionDirectoryInfo(currentPath);
        }    
    }   
}