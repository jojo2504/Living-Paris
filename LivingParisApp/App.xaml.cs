using System;
using System.Windows;
using LivingParisApp.Services;
using LivingParisApp.Core.Engines.ShortestPaths;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.FileHandling;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.EnvironmentSetup.Constants;
using LivingParisApp.Services.MySQL;
using System.IO;
using System.Data;
using System.Text;
using LivingParisApp.Core.Mapping;

namespace LivingParisApp {
    public partial class App : Application {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            Logger.ClearLog();

            string[] args = e.Args;
            string[] validArgs = ["--reset", "--noLogSQL"];
            bool[] bools = Enumerable.Repeat(false, validArgs.Length).ToArray();

            for (int i = 0; i < args.Length; i++) {
                if (validArgs.Contains(args[i])) {
                    bools[i] = true;
                }
            }
            MySQLManager mySQLManager = new MySQLManager(bools);

            //var karateFilePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "assets", "soc-karate.mtx");
            //var graph = new Graph<int>(s => int.Parse(s), karateFilePath); // Populate with integer nodes
            //Logger.Log(graph.DisplayAdjacencyMatrix());
            Logger.Log("generating the map...");

            Map<MetroStation> map = new Map<MetroStation>(
                Path.Combine(GetSolutionDirectoryInfo().FullName, "assets", "MetroParisNoeuds.csv"),
                Path.Combine(GetSolutionDirectoryInfo().FullName, "assets", "MetroParisArcs.csv")
            );

            // Logger.Log(map.ToString());

            try {
                BellmanFord<MetroStation> bellmanFord = new BellmanFord<MetroStation>();
                bellmanFord.Init(map, map.AdjacencyList.Keys.ToList()[0]);
                var (a,b) = bellmanFord.GetPath(map.AdjacencyList.Keys.ToList()[5]);

                Logger.Log($"{a}, {b}");
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }

            Window window = new Window();
            window.Show();
        }
    }
}