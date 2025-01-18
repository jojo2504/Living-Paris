namespace LivingParisApp.Services.EnvironmentSetup{
    public static class Constants {
        private static string DatabaseHost => ConfigLoader.DatabaseHost;
        private static int DatabasePort => ConfigLoader.DatabasePort;
        private static string DatabaseName => ConfigLoader.DatabaseName;
        private static string DatabaseUser => ConfigLoader.DatabaseUser;
        private static string DatabasePassword => ConfigLoader.DatabasePassword;

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