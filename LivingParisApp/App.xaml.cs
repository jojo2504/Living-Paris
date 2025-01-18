using System;
using System.Windows;
using LivingParisApp.Services;
using LivingParisApp.Core.Engines.ShortestPaths;

namespace LivingParisApp {
    public partial class App : Application {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
        }
    }
}
