using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.EnvironmentSetup.Constants;

namespace LivingParisApp.Services.MySQL {
    public class MySQLManager {
        private readonly string _connectionString;
        private bool resetDatabase;

        public MySQLManager(string resetDatabase = "") {
            Logger.Log("Initializing MySQLManager");
            EnsureDatabaseExists(); // Check and create database if needed
            _connectionString = GetConnectionString();
            Logger.Log($"Connection string set: {_connectionString}");

            this.resetDatabase = resetDatabase == "--reset";
            InitializeDatabase(); // Create tables if needed
        }

        private void EnsureDatabaseExists() {
            // Base connection string without database
            string baseConnectionString = $"Server={DatabaseHost};Port={DatabasePort};User={DatabaseUser};Password={DatabasePassword}";
            Logger.Log($"Base connection string: {baseConnectionString}");

            using (var connection = new MySqlConnection(baseConnectionString)) {
                try {
                    connection.Open();
                    Logger.Log("Connected to MySQL server");

                    // Check if database exists and create it if not
                    string query = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}`";
                    using (var command = new MySqlCommand(query, connection)) {
                        command.ExecuteNonQuery();
                        Logger.Log($"Database '{DatabaseName}' ensured (created if it didn't exist)");
                    }
                }
                catch (Exception ex) {
                    Logger.Error($"Failed to ensure database exists: {ex.Message}");
                    throw;
                }
            }
        }

        public bool TestConnection() {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    Logger.Log("Database connection test successful");
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
                Logger.Error("Database connection test failed");
                return false;
            }
        }

        public void ExecuteNonQuery(string query, Dictionary<string, object>? parameters = null) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection)) {
                        if (parameters != null) {
                            foreach (var param in parameters) {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }
                        command.ExecuteNonQuery();
                        Logger.Log($"Executed non-query: {query}");
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {query}, Error: {ex}");
                Logger.Error($"????????????????????????");
                throw;
            }
        }

        public object ExecuteScalar(string query, Dictionary<string, object>? parameters = null) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection)) {
                        if (parameters != null) {
                            foreach (var param in parameters) {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }
                        var result = command.ExecuteScalar();
                        Logger.Log($"Executed scalar query: {query}, Result: {result}");
                        return result;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute scalar query: {query}, Error: {ex.Message}");
                throw;
            }
        }

        public List<DataTable> ExecuteQuery(string query, Dictionary<string, object>? parameters = null) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection)) {
                        if (parameters != null) {
                            foreach (var param in parameters) {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }

                        using (var reader = command.ExecuteReader()) {
                            var resultSets = new List<DataTable>();
                            int resultSetIndex = 0;

                            do {
                                // Skip non-result-set statements (e.g., INSERTs)
                                if (!reader.HasRows && resultSetIndex < 4) // Skip the 4 INSERT/USE statements
                                {
                                    resultSetIndex++;
                                    continue;
                                }

                                var dataTable = new DataTable();
                                // Define columns based on the current result set
                                for (int i = 0; i < reader.FieldCount; i++) {
                                    dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                                }

                                // Read rows
                                while (reader.Read()) {
                                    var row = dataTable.NewRow();
                                    for (int i = 0; i < reader.FieldCount; i++) {
                                        row[i] = reader.GetValue(i);
                                    }
                                    dataTable.Rows.Add(row);
                                }

                                resultSets.Add(dataTable);
                                Logger.Log($"Result set {resultSets.Count} returned: {dataTable.Rows.Count} rows");
                                resultSetIndex++;

                            } while (reader.NextResult());

                            Logger.Log($"Executed query: {query}, Total result sets: {resultSets.Count}");
                            return resultSets;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {query}, Error: {ex.Message}");
                throw;
            }
        }

        public void InitializeDatabase() {
            try {
                if (resetDatabase) {
                    // reset database
                    var resetTablePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "reset_database.sql");
                    var resetTableQuery = File.ReadAllText(resetTablePath);
                    ExecuteNonQuery(resetTableQuery);
                    Logger.Log("Database resetted successfully");
                }
                // Add your table creation queries here
                var createTablePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "create_database.sql");
                var createTableQuery = File.ReadAllText(createTablePath);
                ExecuteNonQuery(createTableQuery);
                Logger.Log("Database initialized successfully with all tables");
            }
            catch (Exception ex) {
                Logger.Error($"Failed to initialize database: {ex.Message}");
                throw;
            }
        }
    }
}