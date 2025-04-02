using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using LivingParisApp.Core.GraphStructure;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using LivingParisApp.Services.Logging;

using LivingParisApp.Core.Models.Human;

using MySql.Data.MySqlClient;
using LivingParisApp.Services.MySQL;
using System.Data;

namespace LivingParisApp {
    public partial class MainWindow : Window {
        MySQLManager _mySQLManager;
        private User _currentUser;

        public MainWindow(MySQLManager mySQLManager) {
            InitializeComponent();

            _mySQLManager = mySQLManager;

            // Set up event handlers
            btnSignIn.Click += BtnSignIn_Click;
            btnSignUp.Click += BtnSignUp_Click;
            btnSignOut.Click += BtnSignOut_Click;

            // Hide the account tab initially
            tabAccount.Visibility = Visibility.Collapsed;

            // Check if there's a saved session
            CheckForSavedSession();
        }

        private void CheckForSavedSession() {
            // This method could check for a saved token or credentials in app settings
            // For now, we'll just assume no saved session
        }

        private void UpdateUIForLoggedInUser() {
            // Show account tab
            tabAccount.Visibility = Visibility.Visible;

            // Update user information display
            txtUserInfo.Text = $"Welcome, {_currentUser.FirstName} {_currentUser.LastName}";

            // Build roles text
            List<string> roles = new List<string>();
            if (_currentUser.IsClient == 1) roles.Add("Client");
            if (_currentUser.IsChef == 1) roles.Add("Chef");
            txtUserRoles.Text = $"Roles: {string.Join(", ", roles)}";

            // Update email display
            txtUserEmail.Text = $"Email: {_currentUser.Mail}";
        }

        private void UpdateUIForLoggedOutUser() {
            // Hide account tab
            tabAccount.Visibility = Visibility.Collapsed;

            // Clear sign in fields
            txtSignInEmail.Text = string.Empty;
            pwdSignIn.Password = string.Empty;
            txtSignInStatus.Text = string.Empty;

            // Switch to the Sign In tab (assuming TabControl is the main control)
            // Get the parent TabControl
            if (tabAccount.Parent is TabControl tabControl) {
                // Select the first tab (Sign In tab)
                tabControl.SelectedIndex = 0;
            }
        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e) {
            string email = txtSignInEmail.Text.Trim();
            string password = pwdSignIn.Password;

            // Basic validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
                txtSignInStatus.Text = "Please enter both email and password";
                return;
            }

            try {
                // Query to get the user ID using the email
                string userQuery = @"
                    SELECT p.UserID, p.FirstName, p.LastName, p.Mail, p.Street, p.StreetNumber, 
                           p.Postcode, p.City, p.PhoneNumber, p.ClosestMetro, p.IsClient, p.IsChef
                    FROM Users p
                    WHERE p.Mail = @Email";

                var userCommand = new MySqlCommand(userQuery);
                userCommand.Parameters.AddWithValue("@Email", email);

                using (var userReader = _mySQLManager.ExecuteReader(userCommand)) {
                    if (userReader is null) {
                        txtSignInStatus.Text = "Signed in not successfully";
                        Logger.Log($"User {email} signed in not successfully");
                        return;
                    }

                    while (userReader.Read()){                    
                        // Query to verify the password
                        string passwordQuery = @"
                                SELECT u.Password
                                FROM Users u
                                WHERE u.UserID = @UserID";

                        var passwordCommand = new MySqlCommand(passwordQuery);
                        passwordCommand.Parameters.AddWithValue("@UserID", (int)userReader["userID"]);
                        var passwordResult = _mySQLManager.ExecuteScalar(passwordCommand);
                        // Check if passwords match (in a real app, use proper password hashing)
                        if (password is null || password != (string)passwordResult) {
                            txtSignInStatus.Text = "Invalid email or password";
                            Logger.Log($"Failed login attempt for user {email}");
                            return;
                        }

                        // Create User object
                        _currentUser = new User {
                            UserID = (int)userReader["UserID"],
                            FirstName = (string)userReader["FirstName"],
                            LastName = (string)userReader["LastName"],
                            Mail = (string)userReader["Mail"],
                            Street = (string)userReader["Street"],
                            StreetNumber = (int)userReader["StreetNumber"],
                            Postcode = (string)userReader["Postcode"],
                            City = (string)userReader["City"],
                            PhoneNumber = (string)userReader["PhoneNumber"],
                            Password = (string)passwordResult,
                            ClosestMetro = userReader["ClosestMetro"] == DBNull.Value ? "" : (string)userReader["ClosestMetro"],
                            IsChef = (int)userReader["IsChef"],
                            IsClient = (int)userReader["IsClient"]
                        };
                    }
                }
                // Update UI for logged in user
                UpdateUIForLoggedInUser();
            }
            catch (Exception ex) {
                Logger.Error($"Login error: {ex}");
                txtSignInStatus.Text = "Error during sign in. Please try again.";
            }
        }

        private void BtnSignUp_Click(object sender, RoutedEventArgs e) {
            // Validate that at least one role is selected
            if (!chkClient.IsChecked.Value && !chkChef.IsChecked.Value) {
                txtSignUpStatus.Text = "Please select at least one role (Client or Chef)";
                return;
            }

            // Validate passwords match
            if (pwdSignUp.Password != pwdConfirm.Password) {
                txtSignUpStatus.Text = "Passwords do not match";
                return;
            }

            // Basic validation for required fields
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(pwdSignUp.Password) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) ||
                string.IsNullOrWhiteSpace(txtStreet.Text) ||
                string.IsNullOrWhiteSpace(txtStreetNumber.Text) ||
                string.IsNullOrWhiteSpace(txtPostcode.Text) ||
                string.IsNullOrWhiteSpace(txtCity.Text)) {
                txtSignUpStatus.Text = "Please fill in all required fields";
                return;
            }

            // Check if email already exists
            if (EmailExists(txtEmail.Text.Trim())) {
                txtSignUpStatus.Text = "Email already in use. Please use a different email.";
                return;
            }

            SaveAccountToDatabase();
        }

        private bool EmailExists(string email) {
            string query = @"
                SELECT COUNT(*)
                FROM Users
                WHERE Mail = @Email";

            var command = new MySqlCommand(query);
            command.Parameters.AddWithValue("@Email", email);

            int count = Convert.ToInt32(_mySQLManager.ExecuteScalar(command));
            return count > 0;
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e) {
            // Clear current user
            _currentUser = null;

            // Update UI for logged out state
            UpdateUIForLoggedOutUser();

            MessageBox.Show("Signed out successfully");
        }

        private void SaveAccountToDatabase() {
            try {
                string userQuery = @"
                    INSERT INTO Users (LastName, FirstName, Street, StreetNumber, Postcode, City, PhoneNumber, Mail, ClosestMetro, Password, IsClient, IsChef)
                    VALUES (@LastName, @FirstName, @Street, @StreetNumber, @Postcode, @City, @PhoneNumber, @Mail, @ClosestMetro, @Password, @IsClient, @IsChef);"
                ;

                var command = new MySqlCommand(userQuery);
                command.Parameters.AddWithValue("@LastName", txtLastName.Text);
                command.Parameters.AddWithValue("@FirstName", txtFirstName.Text);
                command.Parameters.AddWithValue("@Street", txtStreet.Text);
                command.Parameters.AddWithValue("@StreetNumber", int.Parse(txtStreetNumber.Text));
                command.Parameters.AddWithValue("@Postcode", txtPostcode.Text);
                command.Parameters.AddWithValue("@City", txtCity.Text);
                command.Parameters.AddWithValue("@PhoneNumber", txtPhone.Text);
                command.Parameters.AddWithValue("@Mail", txtEmail.Text);
                command.Parameters.AddWithValue("@ClosestMetro", string.IsNullOrWhiteSpace(txtMetro.Text) ? DBNull.Value : txtMetro.Text);
                command.Parameters.AddWithValue("@Password", pwdSignUp.Password);
                command.Parameters.AddWithValue("@IsClient", (bool)chkClient.IsChecked ? 1 : 0);
                command.Parameters.AddWithValue("@IsChef", (bool)chkChef.IsChecked ? 1 : 0);

                _mySQLManager.ExecuteNonQuery(command);

                txtSignUpStatus.Text = $"Account for {txtFirstName.Text} {txtLastName.Text} created successfully";
            }
            catch (Exception ex) {
                Logger.Error($"Error creating account: {ex.Message}");
                txtSignUpStatus.Text = "Error creating account. Please try again.";
            }
        }
    }
}