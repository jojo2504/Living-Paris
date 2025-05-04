using System;
using System.Windows;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.Environment.Constants;
using LivingParisApp.Services.MySQL;
using System.IO;
using System.Data;
using LivingParisApp.Core.Mapping;
using MySql.Data.MySqlClient;
using LivingParisApp.Core.Engines.GraphColoration;
using LivingParisApp.Core.Entities.Station;

namespace LivingParisApp {
    public partial class App : Application {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e) {
            try {
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
                    Path.Combine(CsvAssetsPath, "MetroParisNoeuds.csv"),
                    Path.Combine(CsvAssetsPath, "MetroParisArcs.csv")
                );

                //Logger.Log(map.ToString());
                
                MainWindow window = new MainWindow(mySQLManager, map);
                window.Show();
            }
            catch (Exception ex) {
                Logger.Fatal(ex);
            }
        }
    }
}