using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using LivingParisApp.Services.Logging;
using static LivingParisApp.Services.EnvironmentSetup.Constants;

namespace LivingParisApp.Services.MySQL {
    public class MySQLManager {
        private readonly string _connectionString;

        public MySQLManager() {
            _connectionString = GetConnectionString();
        }

        public bool TestConnection() {
            try {
                using (var connection = new MySqlConnection(_connectionString)) {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error("Database connection test failed", ex);
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
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {query}", ex);
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
                        return command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute scalar query: {query}", ex);
                throw;
            }
        }

        public DataTable ExecuteQuery(string query, Dictionary<string, object>? parameters = null) {
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
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to execute query: {query}", ex);
                throw;
            }
        }

        // Example method to initialize the database
        public void InitializeDatabase() {
            try {
                // Add your table creation queries here
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Username VARCHAR(50) NOT NULL,
                        Email VARCHAR(100) NOT NULL,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                ExecuteNonQuery(createTableQuery);
                Logger.Log("Database initialized successfully");
            }
            catch (Exception ex) {
                Logger.Error("Failed to initialize database", ex);
                throw;
            }
        }
    }
}