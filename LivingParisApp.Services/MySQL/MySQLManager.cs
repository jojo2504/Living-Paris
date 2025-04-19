using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.EnvironmentSetup.Constants;

namespace LivingParisApp.Services.MySQL {
    public class MySQLManager {
        private readonly string _connectionString;
        private readonly bool _resetDatabase;
        private readonly bool _noLogSQLcommand;
        private readonly bool _initMock;

        public MySQLManager(bool[] args) {
            try {
                Logger.Log("Initializing MySQLManager");
                EnsureDatabaseExists(); // Check and create database if needed
                _connectionString = GetConnectionString();
                Logger.Log($"Connection string set: {_connectionString}");

                _resetDatabase = args[0];
                _noLogSQLcommand = args[1];
                _initMock = args[2];

                if (_initMock) {
                    InitializeMockDatabase();
                    Logger.Success("Initialized Mock Database");
                }
                else {
                    InitializeDatabase(); // Create tables if needed
                    Logger.Success("Initialized Normal Database");
                }
            }
            catch (Exception ex) {
                Logger.Fatal(ex);
            }
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

        public void ExecuteNonQuery(MySqlCommand command) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    command.Connection = connection;
                    if (!_noLogSQLcommand) Logger.Log($"Executing non-query: {command.CommandText}");
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {command.CommandText}, Error: {ex}");
                throw;
            }
        }
        public void ExecuteNonQuery(string commandText) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    using (var command = new MySqlCommand(commandText, connection)) {
                        if (!_noLogSQLcommand) Logger.Log($"Executing non-query: {command.CommandText}");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {commandText}, Error: {ex}");
                throw;
            }
        }

        public int ExecuteNonQuery(MySqlCommand command, MySqlTransaction transaction = null) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    command.Connection = connection;
                    
                    if (!_noLogSQLcommand)
                        Logger.Log($"Executing query: {command.CommandText} with parameters: {string.Join(", ", command.Parameters.Cast<MySqlParameter>().Select(p => $"{p.ParameterName}={p.Value}"))}");

                    int rowsAffected = command.ExecuteNonQuery();

                    Logger.Log($"Query affected {rowsAffected} rows");
                    return rowsAffected;
                }
            }
            catch (MySqlException ex) {
                Logger.Error($"Failed to execute query: {command.CommandText}, Error: {ex.Message}, Error Code: {ex.Number}");
                throw;
            }
            catch (Exception ex) {
                Logger.Error($"Unexpected error in ExecuteNonQuery: {command.CommandText}, Error: {ex.Message}");
                throw;
            }
        }

        public object ExecuteScalar(MySqlCommand command) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    command.Connection = connection;
                    if (!_noLogSQLcommand) Logger.Log($"Executing scalar query: {command.CommandText}");
                    var result = command.ExecuteScalar();
                    return result;
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute scalar query: {command.CommandText}, Error: {ex}");
                throw;
            }
        }
        public object ExecuteScalar(string commandText) {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    using (var command = new MySqlCommand(commandText, connection)) {
                        var result = command.ExecuteScalar();
                        if (_noLogSQLcommand) Logger.Log($"Executed non-query: {commandText}");
                        return result;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute scalar query: {commandText}, Error: {ex}");
                throw;
            }
        }

        public MySqlDataReader ExecuteReader(string commandText) {
            try {
                var connection = new MySqlConnection(_connectionString);
                connection.Open();
                var command = new MySqlCommand(commandText, connection);
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {commandText}, Error: {ex.Message}");
                throw;
            }
        }

        public MySqlDataReader ExecuteReader(MySqlCommand command) {
            try {
                var connection = new MySqlConnection(_connectionString);
                connection.Open();
                command.Connection = connection;
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {command.CommandText}, Error: {ex.Message}");
                throw;
            }
        }

        public void InitializeDatabase() {
            try {
                if (_resetDatabase) {
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

        public void InitializeMockDatabase() {
            try {
                // reset database
                var resetTablePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "reset_database.sql");
                var resetTableQuery = File.ReadAllText(resetTablePath);
                ExecuteNonQuery(resetTableQuery);
                Logger.Success("Database resetted successfully");

                // Add your table creation queries here
                var createTablePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "create_database.sql");
                var createTableQuery = File.ReadAllText(createTablePath);
                ExecuteNonQuery(createTableQuery);

                // Add your table creation queries here
                var mockDatabasePath = Path.Combine(GetSolutionDirectoryInfo().FullName, "scripts", "create_mock_database.sql");
                var mockDatabaseQuery = File.ReadAllText(mockDatabasePath);
                ExecuteNonQuery(mockDatabaseQuery);
                Logger.Log("Database initialized successfully with all tables");
            }
            catch (Exception ex) {
                Logger.Error($"Failed to initialize database: {ex.Message}");
                throw;
            }
        }
    }
}