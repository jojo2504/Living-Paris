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
using LivingParisApp.Core.Mapping;

namespace LivingParisApp {
    public partial class MainWindow : Window {
        MySQLManager _mySQLManager;
        private User _currentUser;
        Map<MetroStation> _map;

        private static readonly Dictionary<string, Brush> LineColors = new Dictionary<string, Brush> {
            {"1", Brushes.Yellow},
            {"2", Brushes.Blue},
            {"3", Brushes.Red},
            {"4", Brushes.Green},
            {"5", Brushes.Orange},
            {"6", Brushes.Pink},
            {"7", Brushes.Purple},
            {"8", Brushes.Brown},
            {"9", Brushes.Cyan},
            {"10", Brushes.Lime},
            {"11", Brushes.Magenta},
            {"12", Brushes.Olive},
            {"13", Brushes.Navy},
            {"14", Brushes.Teal},
            // Add more lines as needed
        };

        // Zoom/pan variables
        private Point _lastMousePosition;
        private bool _isDragging = false;
        private double _scale = 1.0;
        private readonly TranslateTransform _translateTransform = new TranslateTransform();
        private readonly ScaleTransform _scaleTransform = new ScaleTransform();
        private readonly TransformGroup _transformGroup = new TransformGroup();

        public MainWindow(MySQLManager mySQLManager, Map<MetroStation> map) {
            InitializeComponent();

            _mySQLManager = mySQLManager;
            _map = map;
            // Set up event handlers
            btnSignIn.Click += BtnSignIn_Click;
            btnSignUp.Click += BtnSignUp_Click;
            btnSignOut.Click += BtnSignOut_Click;

            // Hide the account tab initially
            tabAccount.Visibility = Visibility.Collapsed;

            // Check if there's a saved session
            CheckForSavedSession();
            InitializeMapTransforms();

            // Automatically draw the metro map when window loads
            this.Loaded += (sender, e) => DrawNodes();
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

                    while (userReader.Read()) {
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

        // Replace the existing InitializeMapTransforms method with this fixed version
        private void InitializeMapTransforms() {
            // Clear existing transforms
            _transformGroup.Children.Clear();

            _scaleTransform.ScaleX = _scale;
            _scaleTransform.ScaleY = _scale;
            _translateTransform.X = 0;
            _translateTransform.Y = 0;

            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);

            // We no longer assign transform to metroCanvas here - it will be applied to contentCanvas in DrawNodes

            // Remove existing handlers to avoid duplicates
            metroCanvas.MouseLeftButtonDown -= MetroCanvas_MouseLeftButtonDown;
            metroCanvas.MouseLeftButtonUp -= MetroCanvas_MouseLeftButtonUp;
            metroCanvas.MouseMove -= MetroCanvas_MouseMove;
            metroCanvas.MouseWheel -= MetroCanvas_MouseWheel;

            // Add new handlers
            metroCanvas.MouseLeftButtonDown += MetroCanvas_MouseLeftButtonDown;
            metroCanvas.MouseLeftButtonUp += MetroCanvas_MouseLeftButtonUp;
            metroCanvas.MouseMove += MetroCanvas_MouseMove;
            metroCanvas.MouseWheel += MetroCanvas_MouseWheel;
        }

        private void MetroCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            double zoom = e.Delta > 0 ? 1.1 : 0.9;
            _scale = Math.Max(0.1, Math.Min(5.0, _scale * zoom));

            Point mousePos = e.GetPosition(metroCanvas);

            // Adjust transform origin based on mouse position
            _translateTransform.X = mousePos.X - (mousePos.X - _translateTransform.X) * zoom;
            _translateTransform.Y = mousePos.Y - (mousePos.Y - _translateTransform.Y) * zoom;

            _scaleTransform.ScaleX = _scale;
            _scaleTransform.ScaleY = _scale;

            e.Handled = true;
        }

        // Replace the existing MouseLeftButtonDown handler with this fixed version
        private void MetroCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            // Remove the Ellipse check to allow dragging from anywhere on the canvas
            _lastMousePosition = e.GetPosition(metroCanvas);
            _isDragging = true;
            metroCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void MetroCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && _isDragging) {
                _isDragging = false;
                metroCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void MetroCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (_isDragging) {
                Point currentPosition = e.GetPosition(metroCanvas);
                _translateTransform.X += currentPosition.X - _lastMousePosition.X;
                _translateTransform.Y += currentPosition.Y - _lastMousePosition.Y;
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        private void DrawNodes() {
            try {
                metroCanvas.Children.Clear();

                if (_map?.AdjacencyList == null || _map.AdjacencyList.Count == 0) {
                    return;
                }

                // Get all valid coordinates first
                var validNodes = _map.AdjacencyList.Keys
                    .Where(node => node?.Object != null)
                    .Select(node => new {
                        Node = node,
                        Longitude = node.Object.Longitude,
                        Latitude = node.Object.Latitude
                    }).ToList();

                if (!validNodes.Any()) return;

                // Calculate bounds with some padding
                double minLongitude = validNodes.Min(x => x.Longitude);
                double maxLongitude = validNodes.Max(x => x.Longitude);
                double minLatitude = validNodes.Min(x => x.Latitude);
                double maxLatitude = validNodes.Max(x => x.Latitude);

                // Add 10% padding to the bounds
                double longitudePadding = (maxLongitude - minLongitude) * 0.1;
                double latitudePadding = (maxLatitude - minLatitude) * 0.1;

                minLongitude -= longitudePadding;
                maxLongitude += longitudePadding;
                minLatitude -= latitudePadding;
                maxLatitude += latitudePadding;

                double longitudeRange = maxLongitude - minLongitude;
                double latitudeRange = maxLatitude - minLatitude;

                // Create a content group that will be transformed
                Canvas contentCanvas = new Canvas {
                    Width = 1000,
                    Height = 1000,
                    Background = Brushes.Transparent
                };

                // Draw connections first
                foreach (var stationEntry in _map.AdjacencyList) {
                    if (stationEntry.Key?.Object == null) continue;

                    // Normalize coordinates with padding
                    double x1 = ((stationEntry.Key.Object.Longitude - minLongitude) / longitudeRange) * 1000;
                    double y1 = 1000 - ((stationEntry.Key.Object.Latitude - minLatitude) / latitudeRange) * 1000;

                    foreach (var neighborTuple in stationEntry.Value) {
                        if (neighborTuple?.Item1?.Object == null) continue;

                        double x2 = ((neighborTuple.Item1.Object.Longitude - minLongitude) / longitudeRange) * 1000;
                        double y2 = 1000 - ((neighborTuple.Item1.Object.Latitude - minLatitude) / latitudeRange) * 1000;

                        string line = stationEntry.Key.Object.LibelleLine ?? "default";
                        Brush lineColor = LineColors.TryGetValue(line, out var color) ? color : Brushes.Gray;

                        var connection = new Line {
                            X1 = x1,
                            Y1 = y1,
                            X2 = x2,
                            Y2 = y2,
                            Stroke = lineColor,
                            StrokeThickness = 3,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeStartLineCap = PenLineCap.Round
                        };
                        contentCanvas.Children.Add(connection);
                        Panel.SetZIndex(connection, -1);
                    }
                }

                // Draw stations
                foreach (var stationEntry in _map.AdjacencyList) {
                    if (stationEntry.Key?.Object == null) continue;

                    // Normalize coordinates with padding
                    double x = (stationEntry.Key.Object.Longitude - minLongitude) / longitudeRange * 1000;
                    double y = 1000 - (stationEntry.Key.Object.Latitude - minLatitude) / latitudeRange * 1000;

                    var node = new Ellipse {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.White,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Cursor = Cursors.Hand,
                        Tag = stationEntry.Key.Object.LibelleStation ?? "Unknown"
                    };
                    Canvas.SetLeft(node, x - 6);
                    Canvas.SetTop(node, y - 6);

                    var label = new TextBlock {
                        Text = stationEntry.Key.Object.LibelleStation ?? "Unknown",
                        FontSize = 10,
                        Foreground = Brushes.Black,
                        Background = Brushes.White,
                        Padding = new Thickness(4),
                        Visibility = Visibility.Hidden
                    };
                    Canvas.SetLeft(label, x + 10);
                    Canvas.SetTop(label, y - 10);

                    node.MouseEnter += (s, e) => {
                        label.Visibility = Visibility.Visible;
                        ((Ellipse)s).Fill = Brushes.Yellow;
                    };
                    node.MouseLeave += (s, e) => {
                        label.Visibility = Visibility.Hidden;
                        ((Ellipse)s).Fill = Brushes.White;
                    };

                    contentCanvas.Children.Add(node);
                    contentCanvas.Children.Add(label);
                }

                // Apply the transform to the content canvas, not the main canvas
                contentCanvas.RenderTransform = _transformGroup;

                // Add the content canvas to the main canvas
                metroCanvas.Children.Add(contentCanvas);
            }
            catch (Exception ex) {
                MessageBox.Show($"Error drawing map: {ex.Message}");
            }
        }
    }
}