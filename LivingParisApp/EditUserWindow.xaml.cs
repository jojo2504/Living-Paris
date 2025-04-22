using System;
using System.Collections.ObjectModel;
using System.Windows;
using LivingParisApp.Core.Models.Human;
using LivingParisApp.Services.Logging;
using LivingParisApp.Services.MySQL;
using MySql.Data.MySqlClient;

namespace LivingParisApp {
    public partial class EditUserWindow : Window {
        private readonly MySQLManager _mySQLManager;
        private readonly User _userToEdit;
        
        // Property to store the edited user
        public User EditedUser { get; private set; }

        public EditUserWindow(MySQLManager mySQLManager, User userToEdit, ObservableCollection<string> _allMetroName) {
            InitializeComponent();
            _mySQLManager = mySQLManager;
            _userToEdit = userToEdit;

            if (cmbClosestMetro.ItemsSource == null) {
                cmbClosestMetro.ItemsSource = _allMetroName;
            }
            
            if (_userToEdit != null) {
                LoadUserForEditing();
            }
        }

        private void LoadUserForEditing() {
            txtFirstName.Text = _userToEdit.FirstName;
            txtLastName.Text = _userToEdit.LastName;
            txtStreet.Text = _userToEdit.Street;
            txtStreetNumber.Text = _userToEdit.StreetNumber.ToString();
            txtPostcode.Text = _userToEdit.Postcode;
            txtCity.Text = _userToEdit.City;
            txtPhoneNumber.Text = _userToEdit.PhoneNumber;
            txtMail.Text = _userToEdit.Mail;
            cmbClosestMetro.Text = _userToEdit.ClosestMetro;
            // Password box doesn't show the actual password for security reasons
            // We'll only update password if the user enters a new one
            
            chkIsClient.IsChecked = _userToEdit.IsClient == 1;
            chkIsChef.IsChecked = _userToEdit.IsChef == 1;
            
            // Set the title to show we're editing
            this.Title = $"Edit User: {_userToEdit.FullName}";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            try {
                // Validate inputs
                if (string.IsNullOrEmpty(txtFirstName.Text) ||
                    string.IsNullOrEmpty(txtLastName.Text) ||
                    string.IsNullOrEmpty(txtStreet.Text) ||
                    string.IsNullOrEmpty(txtStreetNumber.Text) ||
                    string.IsNullOrEmpty(txtPostcode.Text) ||
                    string.IsNullOrEmpty(txtCity.Text) ||
                    string.IsNullOrEmpty(txtPhoneNumber.Text) ||
                    string.IsNullOrEmpty(txtMail.Text) ||
                    string.IsNullOrEmpty(cmbClosestMetro.Text)) {
                    MessageBox.Show("Please fill in all required fields",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtStreetNumber.Text, out int streetNumber)) {
                    MessageBox.Show("Please enter a valid street number",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Prepare query - only update password if provided
                string query;
                MySqlCommand command;
                
                if (string.IsNullOrEmpty(txtPassword.Password)) {
                    // Update without changing password
                    query = @"UPDATE Users 
                        SET FirstName = @FirstName, 
                            LastName = @LastName, 
                            Street = @Street, 
                            StreetNumber = @StreetNumber, 
                            Postcode = @Postcode, 
                            City = @City, 
                            PhoneNumber = @PhoneNumber, 
                            Mail = @Mail, 
                            ClosestMetro = @ClosestMetro, 
                            IsClient = @IsClient, 
                            IsChef = @IsChef 
                        WHERE UserID = @UserID";
                    
                    command = new MySqlCommand(query);
                } else {
                    // Update including password
                    query = @"UPDATE Users 
                        SET FirstName = @FirstName, 
                            LastName = @LastName, 
                            Street = @Street, 
                            StreetNumber = @StreetNumber, 
                            Postcode = @Postcode, 
                            City = @City, 
                            PhoneNumber = @PhoneNumber, 
                            Mail = @Mail, 
                            ClosestMetro = @ClosestMetro, 
                            Password = @Password, 
                            IsClient = @IsClient, 
                            IsChef = @IsChef 
                        WHERE UserID = @UserID";
                    
                    command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@Password", txtPassword.Password);
                }

                // Add parameters
                command.Parameters.AddWithValue("@UserID", _userToEdit.UserID);
                command.Parameters.AddWithValue("@FirstName", txtFirstName.Text);
                command.Parameters.AddWithValue("@LastName", txtLastName.Text);
                command.Parameters.AddWithValue("@Street", txtStreet.Text);
                command.Parameters.AddWithValue("@StreetNumber", streetNumber);
                command.Parameters.AddWithValue("@Postcode", txtPostcode.Text);
                command.Parameters.AddWithValue("@City", txtCity.Text);
                command.Parameters.AddWithValue("@PhoneNumber", txtPhoneNumber.Text);
                command.Parameters.AddWithValue("@Mail", txtMail.Text);
                command.Parameters.AddWithValue("@ClosestMetro", cmbClosestMetro.Text);
                command.Parameters.AddWithValue("@IsClient", chkIsClient.IsChecked == true ? 1 : 0);
                command.Parameters.AddWithValue("@IsChef", chkIsChef.IsChecked == true ? 1 : 0);

                // Execute command
                _mySQLManager.ExecuteNonQuery(command);

                // Update the user object
                _userToEdit.FirstName = txtFirstName.Text;
                _userToEdit.LastName = txtLastName.Text;
                _userToEdit.Street = txtStreet.Text;
                _userToEdit.StreetNumber = streetNumber;
                _userToEdit.Postcode = txtPostcode.Text;
                _userToEdit.City = txtCity.Text;
                _userToEdit.PhoneNumber = txtPhoneNumber.Text;
                _userToEdit.Mail = txtMail.Text;
                _userToEdit.ClosestMetro = cmbClosestMetro.Text;
                _userToEdit.IsClient = chkIsClient.IsChecked == true ? 1 : 0;
                _userToEdit.IsChef = chkIsChef.IsChecked == true ? 1 : 0;

                // Set the edited user to return
                EditedUser = _userToEdit;

                Logger.Log($"User updated: {_userToEdit.FullName}");
                DialogResult = true; // Indicate success
                Close();
            }
            catch (Exception ex) {
                Logger.Log($"Error updating user: {ex.Message}");
                MessageBox.Show($"Error saving user: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }
    }
}