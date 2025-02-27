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

namespace LivingParisApp {
    public partial class App : Application {
        // Override OnStartup to call DemonstrateLogging
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            Logger.ClearLog();

            string[] args = e.Args;

            MySQLManager mySQLManager;
            if (args.Length > 0) {
                mySQLManager = new MySQLManager(args[0]);
            }
            else {
                mySQLManager = new MySQLManager();
            }

            try {
                var populateDatabasePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "population_test.sql");
                var populateDatabaseQuery = File.ReadAllText(populateDatabasePath);
                var a = mySQLManager.ExecuteQuery(populateDatabaseQuery);
                // Log the results meaningfully
                Logger.Important($"{a.GetType()}");
                Logger.Important($"Total result sets returned: {a.Count}");

                // Create dictionary to store the person data
                Dictionary<int, DataRow> personsDict = new Dictionary<int, DataRow>();

                // Process Persons table first to build the dictionary
                if (a.Count > 0) {
                    DataTable personsTable = a[0];
                    Logger.Important($"Persons (Result set 1): {personsTable.Rows.Count} rows");

                    foreach (DataRow row in personsTable.Rows) {
                        int personId = Convert.ToInt32(row["PersonID"]);
                        personsDict[personId] = row;

                        // Log each person
                        var values = new List<string>();
                        for (int j = 0; j < personsTable.Columns.Count; j++) {
                            values.Add($"{personsTable.Columns[j].ColumnName}: {row[j]}");
                        }
                        Logger.Important($"Person {personId}: {string.Join(", ", values)}");
                    }
                }

                // Process Chefs table and display with person data
                if (a.Count > 1) {
                    DataTable chefsTable = a[1];
                    Logger.Important($"Chefs (Result set 2): {chefsTable.Rows.Count} rows");

                    foreach (DataRow chefRow in chefsTable.Rows) {
                        int personId = Convert.ToInt32(chefRow["PersonID"]);

                        // Display chef data
                        StringBuilder chefInfo = new StringBuilder();
                        chefInfo.Append($"Chef (PersonID: {personId}): ");

                        // Add chef-specific columns
                        List<string> chefColumns = new List<string>();
                        foreach (DataColumn col in chefRow.Table.Columns) {
                            if (col.ColumnName != "PersonID") { // Skip PersonID since we're already showing it
                                chefColumns.Add($"{col.ColumnName}: {chefRow[col]}");
                            }
                        }

                        if (chefColumns.Count > 0) {
                            chefInfo.Append(string.Join(", ", chefColumns));
                        }
                        else {
                            chefInfo.Append("No additional chef data");
                        }

                        // Add person data
                        if (personsDict.ContainsKey(personId)) {
                            DataRow personRow = personsDict[personId];
                            chefInfo.Append(" | Person data: ");

                            List<string> personColumns = new List<string>();
                            foreach (DataColumn col in personRow.Table.Columns) {
                                if (col.ColumnName != "PersonID") { // Skip PersonID since we're already showing it
                                    personColumns.Add($"{col.ColumnName}: {personRow[col]}");
                                }
                            }

                            chefInfo.Append(string.Join(", ", personColumns));
                        }
                        else {
                            chefInfo.Append(" | Person data not found");
                        }

                        Logger.Important(chefInfo.ToString());
                    }
                }

                // Process Clients table and display with person data
                if (a.Count > 2) {
                    DataTable clientsTable = a[2];
                    Logger.Important($"Clients (Result set 3): {clientsTable.Rows.Count} rows");

                    foreach (DataRow clientRow in clientsTable.Rows) {
                        int personId = Convert.ToInt32(clientRow["PersonID"]);

                        // Display client data
                        StringBuilder clientInfo = new StringBuilder();
                        clientInfo.Append($"Client (PersonID: {personId}): ");

                        // Add client-specific columns
                        List<string> clientColumns = new List<string>();
                        foreach (DataColumn col in clientRow.Table.Columns) {
                            if (col.ColumnName != "PersonID") { // Skip PersonID since we're already showing it
                                clientColumns.Add($"{col.ColumnName}: {clientRow[col]}");
                            }
                        }

                        if (clientColumns.Count > 0) {
                            clientInfo.Append(string.Join(", ", clientColumns));
                        }
                        else {
                            clientInfo.Append("No additional client data");
                        }

                        // Add person data
                        if (personsDict.ContainsKey(personId)) {
                            DataRow personRow = personsDict[personId];
                            clientInfo.Append(" | Person data: ");

                            List<string> personColumns = new List<string>();
                            foreach (DataColumn col in personRow.Table.Columns) {
                                if (col.ColumnName != "PersonID") { // Skip PersonID since we're already showing it
                                    personColumns.Add($"{col.ColumnName}: {personRow[col]}");
                                }
                            }

                            clientInfo.Append(string.Join(", ", personColumns));
                        }
                        else {
                            clientInfo.Append(" | Person data not found");
                        }

                        Logger.Important(clientInfo.ToString());
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }

            var graph = new Graph<int>(s => int.Parse(s)); // Populate with integer nodes
            //Logger.Log(graph.DisplayAdjacencyMatrix());

            var window = new MainWindow(graph);
            window.Show();
        }
    }
}