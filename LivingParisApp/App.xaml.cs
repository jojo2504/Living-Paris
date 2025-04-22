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
            Logger.Log(string.Join(" ", args));

            string[] validArgs = ["--reset", "--noLogSQL", "--initMock"];
            bool[] bools = Enumerable.Repeat(false, validArgs.Length).ToArray();

            foreach (string arg in args) {
                int index = Array.IndexOf(validArgs, arg);
                if (index != -1) {
                    bools[index] = true;
                }
            }

            Logger.Log($"{bools[0]} {bools[1]} {bools[2]}");

            MySQLManager mySQLManager = new MySQLManager(bools);

            Logger.Log("generating the map...");
            Map<MetroStation> map = new Map<MetroStation>(
                Path.Combine(GetSolutionDirectoryInfo().FullName, "assets", "MetroParisNoeuds.csv"),
                Path.Combine(GetSolutionDirectoryInfo().FullName, "assets", "MetroParisArcs.csv")
            );

            //Logger.Log(map.ToString());

            try {
                MainWindow window = new MainWindow(mySQLManager, map);
                window.Show();
            }
            catch (Exception ex) {
                Logger.Fatal(ex);
            }
        }
    }
}