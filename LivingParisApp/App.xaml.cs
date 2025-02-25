using System;
using System.Windows;
using LivingParisApp.Services;
using LivingParisApp.Core.Engines.ShortestPaths;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.EnvironmentSetup.Constants;

namespace LivingParisApp {
    public partial class App : Application {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            Logger.ClearLog();
            var graph = new Graph<int>(s => int.Parse(s)); // Populate with integer nodes
            var window = new MainWindow(graph);
            window.Show();
        }
    }
}
