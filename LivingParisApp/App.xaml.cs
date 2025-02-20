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
            Graph<string> graph = new Graph<string>();
            FileReader fileReader = new FileReader("C:/Users/jojod/Documents/c#/Living-Paris/assets/soc-karate.mtx");
            foreach (string line in fileReader.ReadLines()) {
                try {
                    var parts = line.Split(' ');
                    //Logger.Log($"{line}, {parts.Length}");
                    if (!int.TryParse(parts[0], out _)) continue;

                    if (parts.Length != 3) {
                        graph.AddEdge(parts[0], parts[1], 1);
                    }
                    else {
                        graph.AddEdge(parts[0], parts[1], int.Parse(parts[2]));
                    }
                }
                catch (Exception ex) {
                    Logger.Fatal(ex);
                }
            }
            try {
                Logger.Log(graph.ToString());
            }
            catch (Exception ex){
                Logger.Error(ex);
            }

            var window = new MainWindow(graph);
            window.Show();
        }
    }
}
