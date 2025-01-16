using System;
using System.Windows;
using LivingParisApp.Services;

namespace LivingParisApp
{
    public partial class App : Application
    {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Call DemonstrateLogging when the app starts
                DemonstrateLogging();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Application encountered a fatal error during startup", ex);
                Environment.Exit(1);  // Optional: exit if a fatal error occurs
            }
        }

        public static void DemonstrateLogging()
        {
            try
            {
                // Regular information logging
                Logger.Log("Starting the application...");

                // Success message
                Logger.Success("User configuration loaded successfully");

                // Warning example
                Logger.Warning("Database connection is slow, might need optimization");

                // Simulating some work
                ProcessSomeWork();

                // Another success message
                Logger.Success("All operations completed successfully");
            }
            catch (Exception ex)
            {
                // Log error with exception
                Logger.Error("An error occurred during demonstration", ex);
            }
        }

        public static void ProcessSomeWork()
        {
            try
            {
                // Simulate some processing
                Logger.Log("Processing started...");

                // Simulate a warning condition
                if (DateTime.Now.Hour >= 23)
                {
                    Logger.Warning("Processing during system maintenance window");
                }

                // Simulate an error condition
                if (new Random().Next(0, 10) == 0)
                {
                    throw new Exception("Random processing error occurred");
                }

                Logger.Success("Processing completed successfully");
            }
            catch (Exception ex)
            {
                // Log a fatal error that requires immediate attention
                Logger.Fatal("Critical error during processing", ex);
                throw; // Re-throw to be handled by caller
            }
        }

        public void CleanupExample()
        {
            try
            {
                Logger.Log("Starting cleanup process...");

                // Simulate cleanup work
                System.Threading.Thread.Sleep(1000);

                // Clear the log file if it's getting too big
                Logger.ClearLog();

                Logger.Success("Cleanup completed");
            }
            catch (Exception ex)
            {
                Logger.Error("Cleanup process failed", ex);
            }
        }
    }
}
