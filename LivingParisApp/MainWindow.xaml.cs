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
using System.Collections.ObjectModel;
using LivingParisApp.Core.Models.Food;
using LivingParisApp.Core.Models.OrderInfo;
using LivingParisApp.Core.Engines.ShortestPaths;

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

        // Observable Collections
        private ObservableCollection<Dish> _availableDishes = new();  // Browse and Order tab
        private ObservableCollection<CartItem> _cartItems = new();    // Shopping cart
        private ObservableCollection<Dish> _myDishes = new();         // Manage My Dishes tab
        private ObservableCollection<Order> _orders = new();          // My Orders tab

        public MainWindow(MySQLManager mySQLManager, Map<MetroStation> map) {
            InitializeComponent();

            _mySQLManager = mySQLManager;
            _map = map;

            // Populate the Closest Metro ComboBox with station names
            if (_map?.AdjacencyList != null) {
                var stationNames = _map.AdjacencyList.Keys
                    .Where(vertex => vertex?.Object?.LibelleStation != null)
                    .Select(vertex => vertex.Object.LibelleStation)
                    .Distinct() // Remove duplicates, if any
                    .OrderBy(name => name); // Sort alphabetically for better usability
                foreach (var stationName in stationNames) {
                    cmbMetro.Items.Add(stationName);
                }
            }

            // Set up event handlers
            btnSignIn.Click += BtnSignIn_Click;
            btnSignUp.Click += BtnSignUp_Click;
            btnSignOut.Click += BtnSignOut_Click;

            // Initialize data bindings
            dgDishes.ItemsSource = _availableDishes;
            lbCart.ItemsSource = _cartItems;
            dgMyDishes.ItemsSource = _myDishes;
            dgOrders.ItemsSource = _orders;

            // Hide tabs initially
            tabAccount.Visibility = Visibility.Collapsed;
            tabFoodServices.Visibility = Visibility.Collapsed;
            metroMap.Visibility = Visibility.Collapsed;

            CheckForSavedSession();
            InitializeMapTransforms();
            LoadInitialData();

            this.Loaded += (sender, e) => DrawNodes();
        }

        private void LoadInitialData() {
            LoadAvailableDishes();
            LoadMyDishes();
            LoadOrders();
        }

        #region Authentification
        private void CheckForSavedSession() {
            // This method could check for a saved token or credentials in app settings
            // For now, we'll just assume no saved session
        }

        private void UpdateUIForLoggedInUser() {
            // Show account tab
            tabAccount.Visibility = Visibility.Visible;
            tabSignIn.Visibility = Visibility.Collapsed;
            tabSignUp.Visibility = Visibility.Collapsed;
            tabFoodServices.Visibility = Visibility.Visible;
            metroMap.Visibility = Visibility.Visible;

            LoadMyDishes();
            LoadOrders();

            //food services tab based on roles
            if (_currentUser.IsChef == 0) {
                tabManageDishes.Visibility = Visibility.Collapsed;
            }
            else {
                tabManageDishes.Visibility = Visibility.Visible;
            }

            //food services tab based on roles
            if (_currentUser.IsClient == 0) {
                tabBrowseOrder.Visibility = Visibility.Collapsed;
            }
            else {
                tabBrowseOrder.Visibility = Visibility.Visible;
            }

            // Update user information display
            txtUserInfo.Text = $"Welcome, {_currentUser.FirstName} {_currentUser.LastName}";
            txtUserEmail.Text = $"Email: {_currentUser.Mail}";
            txtClosestMetro.Text = $"Closest Metro: {_currentUser.ClosestMetro}";

            // Update role checkboxes
            chkAccountClient.IsChecked = _currentUser.IsClient == 1;
            chkAccountChef.IsChecked = _currentUser.IsChef == 1;

            // Ensure controls are in default state
            chkAccountClient.IsEnabled = false;
            chkAccountChef.IsEnabled = false;
            btnEditRoles.Visibility = Visibility.Visible;
            btnSaveRoles.Visibility = Visibility.Collapsed;
            txtRoleUpdateStatus.Text = "";

            // Switch to the Sign In tab (assuming TabControl is the main control)
            // Get the parent TabControl
            if (tabAccount.Parent is TabControl tabControl) {
                // Select the first order food tab
                tabControl.SelectedIndex = 4;
            }
        }

        private void UpdateUIForLoggedOutUser() {
            // Hide account tab
            tabAccount.Visibility = Visibility.Collapsed;
            tabSignIn.Visibility = Visibility.Visible;
            tabSignUp.Visibility = Visibility.Visible;
            tabFoodServices.Visibility = Visibility.Collapsed;
            metroMap.Visibility = Visibility.Collapsed;

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
                        UpdateUIForLoggedInUser();
                    }
                }
                // Update UI for logged in user
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
                command.Parameters.AddWithValue("@ClosestMetro", string.IsNullOrWhiteSpace(cmbMetro.Text) ? DBNull.Value : cmbMetro.Text);
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

        #endregion

        #region Map
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

        private void DrawNodes(LinkedList<Node<MetroStation>> path = null) {
            try {
                metroCanvas.Children.Clear();

                if (_map?.AdjacencyList == null || _map.AdjacencyList.Count == 0) {
                    return;
                }

                // Get all valid coordinates first
                var validNodes = _map.AdjacencyList.Keys
                    .Where(node => node?.Object != null && node.Object.Longitude != 0 && node.Object.Latitude != 0)
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

                // Convert path to a list of edges (pairs of consecutive nodes)
                var pathEdges = new List<(Node<MetroStation> Start, Node<MetroStation> End)>();
                if (path != null && path.Count > 1) {
                    var current = path.First;
                    while (current != null && current.Next != null) {
                        pathEdges.Add((current.Value, current.Next.Value));
                        current = current.Next;
                    }
                }

                // Create a dictionary to store calculated coordinates to avoid recalculating
                var nodeCoordinates = new Dictionary<Node<MetroStation>, (double X, double Y)>();

                // Pre-calculate coordinates for all nodes - corrected mapping
                foreach (var node in _map.AdjacencyList.Keys) {
                    if (node?.Object == null || node.Object.Longitude == 0 || node.Object.Latitude == 0) continue;

                    // Fix the coordinate mapping - longitude maps to X, latitude to Y
                    // For Paris metro map, we need to properly orient it
                    double x = ((node.Object.Longitude - minLongitude) / longitudeRange) * 1000;
                    double y = ((maxLatitude - node.Object.Latitude) / latitudeRange) * 1000;

                    // Ensure coordinates are within canvas bounds
                    x = Math.Max(6, Math.Min(994, x));
                    y = Math.Max(6, Math.Min(994, y));

                    nodeCoordinates[node] = (x, y);
                }

                // Draw connections first
                foreach (var stationEntry in _map.AdjacencyList) {
                    if (stationEntry.Key?.Object == null) continue;

                    // Skip if we don't have coordinates for this node
                    if (!nodeCoordinates.TryGetValue(stationEntry.Key, out var coords1)) continue;
                    double x1 = coords1.X;
                    double y1 = coords1.Y;

                    foreach (var neighborTuple in stationEntry.Value) {
                        if (neighborTuple?.Item1?.Object == null) continue;

                        // Skip if we don't have coordinates for neighbor node
                        if (!nodeCoordinates.TryGetValue(neighborTuple.Item1, out var coords2)) continue;
                        double x2 = coords2.X;
                        double y2 = coords2.Y;

                        // Check if this edge is part of the path
                        bool isPathEdge = pathEdges.Any(edge =>
                            (edge.Start == stationEntry.Key && edge.End == neighborTuple.Item1) ||
                            (edge.Start == neighborTuple.Item1 && edge.End == stationEntry.Key));

                        // Use yellow for path edges, otherwise use the line color
                        Brush edgeColor = isPathEdge ? Brushes.Yellow : (LineColors.TryGetValue(stationEntry.Key.Object.LibelleLine ?? "default", out var color) ? color : Brushes.Gray);

                        var connection = new Line {
                            X1 = x1,
                            Y1 = y1,
                            X2 = x2,
                            Y2 = y2,
                            Stroke = edgeColor,
                            StrokeThickness = isPathEdge ? 5 : 3, // Thicker line for path edges
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeStartLineCap = PenLineCap.Round
                        };
                        contentCanvas.Children.Add(connection);
                        Panel.SetZIndex(connection, isPathEdge ? 1 : -1); // Ensure path edges are on top
                    }
                }

                // Draw stations
                foreach (var stationEntry in _map.AdjacencyList) {
                    if (stationEntry.Key?.Object == null) continue;

                    // Skip if we don't have coordinates for this node
                    if (!nodeCoordinates.TryGetValue(stationEntry.Key, out var coords)) continue;
                    double x = coords.X;
                    double y = coords.Y;

                    // Highlight stations that are part of the path
                    bool isPathStation = path != null && path.Contains(stationEntry.Key);
                    var node = new Ellipse {
                        Width = 12,
                        Height = 12,
                        Fill = isPathStation ? Brushes.Yellow : Brushes.White,
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
                        ((Ellipse)s).Fill = isPathStation ? Brushes.Yellow : Brushes.White;
                    };

                    contentCanvas.Children.Add(node);
                    contentCanvas.Children.Add(label);
                }

                // Apply the transform to the content canvas
                contentCanvas.RenderTransform = _transformGroup;

                // Add the content canvas to the main canvas
                metroCanvas.Children.Add(contentCanvas);
            }
            catch (Exception ex) {
                MessageBox.Show($"Error drawing map: {ex.Message}");
            }
        }

        #endregion

        #region my account role logic
        private void BtnEditRoles_Click(object sender, RoutedEventArgs e) {
            // Enable role checkboxes for editing
            chkAccountClient.IsEnabled = true;
            chkAccountChef.IsEnabled = true;

            // Show save button, hide edit button
            btnSaveRoles.Visibility = Visibility.Visible;
            btnEditRoles.Visibility = Visibility.Collapsed;

            txtRoleUpdateStatus.Text = "Modify your roles and click Save";
            txtRoleUpdateStatus.Foreground = Brushes.Black;
        }

        private void BtnSaveRoles_Click(object sender, RoutedEventArgs e) {
            try {
                if (!chkAccountClient.IsChecked.Value && !chkAccountChef.IsChecked.Value) {
                    txtRoleUpdateStatus.Text = "Error updating roles. Please choose at least one.";
                    txtRoleUpdateStatus.Foreground = Brushes.Red;
                    return;
                }
                // Update user roles in database
                string updateQuery = @"
            UPDATE Users 
            SET IsClient = @IsClient, IsChef = @IsChef 
            WHERE UserID = @UserID";

                var command = new MySqlCommand(updateQuery);
                command.Parameters.AddWithValue("@IsClient", chkAccountClient.IsChecked.Value ? 1 : 0);
                command.Parameters.AddWithValue("@IsChef", chkAccountChef.IsChecked.Value ? 1 : 0);
                command.Parameters.AddWithValue("@UserID", _currentUser.UserID);

                _mySQLManager.ExecuteNonQuery(command);

                // Update current user object
                _currentUser.IsClient = chkAccountClient.IsChecked.Value ? 1 : 0;
                _currentUser.IsChef = chkAccountChef.IsChecked.Value ? 1 : 0;

                // Disable editing
                chkAccountClient.IsEnabled = false;
                chkAccountChef.IsEnabled = false;

                // Show edit button, hide save button
                btnEditRoles.Visibility = Visibility.Visible;
                btnSaveRoles.Visibility = Visibility.Collapsed;

                // Update UI
                UpdateUIForLoggedInUser();

                txtRoleUpdateStatus.Text = "Roles updated successfully";
                txtRoleUpdateStatus.Foreground = Brushes.Green;
            }
            catch (Exception ex) {
                Logger.Error($"Error updating roles: {ex.Message}");
                txtRoleUpdateStatus.Text = "Error updating roles. Please try again.";
                txtRoleUpdateStatus.Foreground = Brushes.Red;
            }
        }

        #endregion

        #region Loader
        private void LoadAvailableDishes() {
            try {
                _availableDishes.Clear();

                string query = @"
                    SELECT d.*, u.FirstName, u.LastName
                    FROM Dishes d 
                    JOIN Users u ON d.ChefID = u.UserID 
                    WHERE d.PeremptionDate > NOW()";
                using (var reader = _mySQLManager.ExecuteReader(query)) {
                    while (reader.Read()) {
                        _availableDishes.Add(new Dish {
                            DishID = reader.GetInt32("DishID"),
                            ChefID = reader.GetInt32("ChefID"),
                            Name = reader.GetString("Name"),
                            Type = reader.GetString("Type"),
                            DishPrice = reader.GetDecimal("DishPrice"),
                            FabricationDate = reader.GetDateTime("FabricationDate"),
                            PeremptionDate = reader.GetDateTime("PeremptionDate"),
                            Diet = reader.IsDBNull(reader.GetOrdinal("Diet")) ? "" : reader.GetString("Diet"),
                            Origin = reader.IsDBNull(reader.GetOrdinal("Origin")) ? "" : reader.GetString("Origin"),
                            ChefName = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}"
                        });
                    }
                }
                Logger.Log($"Loaded {_availableDishes.Count} available dishes");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading available dishes: {ex.Message}");
                MessageBox.Show($"Error loading dishes: {ex.Message}");
            }
        }

        private void LoadMyDishes() {
            try {
                if (_currentUser == null || _currentUser.IsChef == 0) return;

                _myDishes.Clear();
                string query = "SELECT * FROM Dishes WHERE ChefID = @ChefID";
                var command = new MySqlCommand(query);
                command.Parameters.AddWithValue("@ChefID", _currentUser.UserID);

                using (var reader = _mySQLManager.ExecuteReader(command)) {
                    while (reader.Read()) {
                        // Validate required fields
                        string name = reader.GetString("Name");
                        string type = reader.GetString("Type");
                        DateTime fabricationDate = reader.IsDBNull(reader.GetOrdinal("FabricationDate")) ? DateTime.MinValue : reader.GetDateTime("FabricationDate");
                        DateTime peremptionDate = reader.IsDBNull(reader.GetOrdinal("PeremptionDate")) ? DateTime.MinValue : reader.GetDateTime("PeremptionDate");

                        // Skip rows with invalid data
                        if (string.IsNullOrWhiteSpace(name) ||
                            string.IsNullOrWhiteSpace(type) ||
                            fabricationDate == DateTime.MinValue ||
                            peremptionDate == DateTime.MinValue ||
                            fabricationDate.Year < 2000 || // Ensure dates are reasonable
                            peremptionDate.Year < 2000) {
                            Logger.Log($"Skipping invalid dish with ID {reader.GetInt32("DishID")}: Name={name}, Type={type}, FabricationDate={fabricationDate}, PeremptionDate={peremptionDate}");
                            continue;
                        }

                        _myDishes.Add(new Dish {
                            DishID = reader.GetInt32("DishID"),
                            ChefID = _currentUser.UserID,
                            Name = name,
                            Type = type,
                            DishPrice = reader.GetDecimal("DishPrice"),
                            FabricationDate = fabricationDate,
                            PeremptionDate = peremptionDate,
                            Diet = reader.IsDBNull(reader.GetOrdinal("Diet")) ? "" : reader.GetString("Diet"),
                            Origin = reader.IsDBNull(reader.GetOrdinal("Origin")) ? "" : reader.GetString("Origin"),
                        });
                    }
                }
                Logger.Log($"Loaded {_myDishes.Count} chef dishes");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading my dishes: {ex.Message}");
                MessageBox.Show($"Error loading my dishes: {ex.Message}");
            }
        }

        private void LoadOrders() {
            try {
                _orders.Clear();
                string query = cmbOrderView.SelectedIndex == 0
                    ? @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal,
                       uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                       uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                FROM Orders o
                JOIN Users uc ON o.ClientID = uc.UserID
                JOIN Users uch ON o.ChefID = uch.UserID
                WHERE o.ClientID = @UserID"
                    : @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal,
                       uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                       uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                FROM Orders o
                JOIN Users uc ON o.ClientID = uc.UserID
                JOIN Users uch ON o.ChefID = uch.UserID
                WHERE o.ChefID = @UserID";

                var command = new MySqlCommand(query);
                command.Parameters.AddWithValue("@UserID", _currentUser?.UserID ?? 0);

                using (var reader = _mySQLManager.ExecuteReader(command)) {
                    while (reader.Read()) {
                        _orders.Add(new Order {
                            OrderID = reader.GetInt32("OrderID"),
                            ClientID = reader.GetInt32("ClientID"),
                            ChefID = reader.GetInt32("ChefID"),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString("Address"),
                            OrderDate = reader.GetDateTime("OrderDate"),
                            OrderTotal = reader.GetDecimal("OrderTotal"),
                            ClientName = $"{reader.GetString("ClientFirst")} {reader.GetString("ClientLast")}",
                            ChefName = $"{reader.GetString("ChefFirst")} {reader.GetString("ChefLast")}"
                        });
                    }
                }
                Logger.Log($"Loaded {_orders.Count} orders");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading orders: {ex.Message}");
                MessageBox.Show($"Error loading orders: {ex.Message}");
            }
        }

        #endregion

        #region Click
        public void BtnAddNewDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("adding new dish");
            var addWindow = new AddNewDishWindow(_mySQLManager, _currentUser);
            if (addWindow.ShowDialog() == true) {
                LoadAvailableDishes(); // Refresh the data after adding
                dgMyDishes.Items.Refresh(); // Update the DataGrid
                LoadMyDishes();
            }
        }

        public void BtnPlaceOrder_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Place order button clicked");

            // Check if the cart is empty
            if (_cartItems.Count == 0) {
                MessageBox.Show("Your cart is empty. Please add items to place an order.");
                return;
            }

            try {
                // Step 1: Determine the address
                string orderAddress;
                if (rbMyAddress.IsChecked == true) {
                    // Use the user's registered address
                    orderAddress = $"{_currentUser.Street} {_currentUser.StreetNumber}, {_currentUser.Postcode} {_currentUser.City}";
                }
                else {
                    // Use the custom address
                    if (string.IsNullOrWhiteSpace(txtOrderStreet.Text) ||
                        string.IsNullOrWhiteSpace(txtOrderStreetNumber.Text) ||
                        string.IsNullOrWhiteSpace(txtOrderPostcode.Text) ||
                        string.IsNullOrWhiteSpace(txtOrderCity.Text)) {
                        MessageBox.Show("Please fill in all custom address fields.");
                        return;
                    }
                    orderAddress = $"{txtOrderStreet.Text} {txtOrderStreetNumber.Text}, {txtOrderPostcode.Text} {txtOrderCity.Text}";
                }

                // Step 2: Check if all items in the cart belong to the same chef
                int chefId = _cartItems[0].Dish.ChefID;
                if (_cartItems.Any(item => item.Dish.ChefID != chefId)) {
                    MessageBox.Show("All items in the cart must be from the same chef. Please place separate orders for items from different chefs.");
                    return;
                }

                // Step 3: Calculate the order total
                decimal orderTotal = _cartItems.Sum(item => item.TotalPrice);

                // Step 4: Insert the order into the Orders table
                string insertOrderQuery = @"
            INSERT INTO Orders (ClientID, ChefID, Address, OrderDate, OrderTotal)
            VALUES (@ClientID, @ChefID, @Address, @OrderDate, @OrderTotal);
            SELECT LAST_INSERT_ID();";
                var orderCommand = new MySqlCommand(insertOrderQuery);
                orderCommand.Parameters.AddWithValue("@ClientID", _currentUser.UserID);
                orderCommand.Parameters.AddWithValue("@ChefID", chefId);
                orderCommand.Parameters.AddWithValue("@Address", orderAddress);
                orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                orderCommand.Parameters.AddWithValue("@OrderTotal", orderTotal);

                int orderId = Convert.ToInt32(_mySQLManager.ExecuteScalar(orderCommand));
                Logger.Log($"Created order with OrderID: {orderId}");

                // Step 5: Insert each cart item into the OrderDishes table
                foreach (var cartItem in _cartItems) {
                    string insertOrderDishQuery = @"
                INSERT INTO OrderDishes (OrderID, DishID, Quantity)
                VALUES (@OrderID, @DishID, @Quantity)";
                    var orderDishCommand = new MySqlCommand(insertOrderDishQuery);
                    orderDishCommand.Parameters.AddWithValue("@OrderID", orderId);
                    orderDishCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);
                    orderDishCommand.Parameters.AddWithValue("@Quantity", cartItem.Quantity);

                    _mySQLManager.ExecuteNonQuery(orderDishCommand);
                    Logger.Log($"Added dish {cartItem.Dish.DishID} to OrderDishes with quantity {cartItem.Quantity}");
                }

                // Step 6: Remove the dishes from the Dishes table
                foreach (var cartItem in _cartItems) {
                    string deleteDishQuery = "DELETE FROM Dishes WHERE DishID = @DishID";
                    var deleteCommand = new MySqlCommand(deleteDishQuery);
                    deleteCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);

                    _mySQLManager.ExecuteNonQuery(deleteCommand);
                    Logger.Log($"Removed dish {cartItem.Dish.DishID} from Dishes table");
                }

                // Step 7: Update the UI
                _cartItems.Clear();
                UpdateCartTotal();
                LoadOrders();
                LoadAvailableDishes(); // Refresh the available dishes list
                MessageBox.Show("Order placed successfully!");
            }
            catch (Exception ex) {
                Logger.Log($"Error placing order: {ex.Message}");
                MessageBox.Show($"Error placing order: {ex.Message}");
            }
        }
        public void BtnAddToCart_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Add to cart button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                var dish = _availableDishes.FirstOrDefault(d => d.DishID == dishId);
                if (dish != null) {
                    var existingItem = _cartItems.FirstOrDefault(i => i.Dish.DishID == dishId);
                    if (existingItem != null) {
                        existingItem.Quantity++;
                    }
                    else {
                        _cartItems.Add(new CartItem { Dish = dish, Quantity = 1 });
                    }
                    UpdateCartTotal();
                }
            }
        }

        public void BtnEditDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Edit dish button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                var dish = _myDishes.FirstOrDefault(d => d.DishID == dishId);
                if (dish != null) {
                    var editWindow = new AddNewDishWindow(_mySQLManager, _currentUser, dish);
                    if (editWindow.ShowDialog() == true) {
                        LoadMyDishes();
                        LoadAvailableDishes();
                    }
                }
            }
        }

        public void BtnDeleteDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Delete dish button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                if (MessageBox.Show("Are you sure you want to delete this dish?",
                    "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    try {
                        string query = "DELETE FROM Dishes WHERE DishID = @DishID";
                        var command = new MySqlCommand(query);
                        command.Parameters.AddWithValue("@DishID", dishId);
                        _mySQLManager.ExecuteNonQuery(command);

                        LoadMyDishes();
                        LoadAvailableDishes();
                        MessageBox.Show("Dish deleted successfully");
                    }
                    catch (Exception ex) {
                        Logger.Log($"Error deleting dish: {ex.Message}");
                        MessageBox.Show($"Error deleting dish: {ex.Message}");
                    }
                }
            }
        }

        public void BtnViewOrderDetails_Click(object sender, RoutedEventArgs e) {
            Logger.Log("View order details button clicked");

            if (sender is Button button && button.DataContext is Order selectedOrder) {
                try {
                    // Step 1: Retrieve the list of dishes in the order
                    var orderDishes = new List<(string DishName, int Quantity, decimal Price)>();
                    string query = @"
                SELECT od.DishID, od.Quantity, d.Name, d.DishPrice
                FROM OrderDishes od
                JOIN Dishes d ON od.DishID = d.DishID
                WHERE od.OrderID = @OrderID";
                    var command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@OrderID", selectedOrder.OrderID);

                    using (var reader = _mySQLManager.ExecuteReader(command)) {
                        while (reader.Read()) {
                            orderDishes.Add((
                                DishName: reader.GetString("Name"),
                                Quantity: reader.GetInt32("Quantity"),
                                Price: reader.GetDecimal("DishPrice")
                            ));
                        }
                    }

                    // Step 2: Display order details
                    string details = $"Order ID: {selectedOrder.OrderID}\n" +
                                    $"Date: {selectedOrder.OrderDate:dd/MM/yyyy}\n" +
                                    $"Client: {selectedOrder.ClientName}\n" +
                                    $"Chef: {selectedOrder.ChefName}\n" +
                                    $"Total: {selectedOrder.OrderTotal:C2}\n\n" +
                                    "Dishes:\n";
                    foreach (var dish in orderDishes) {
                        details += $"- {dish.DishName} (x{dish.Quantity}) @ {dish.Price:C2} each\n";
                    }

                    MessageBox.Show(details, "Order Details");

                    // Step 3: Get the closest metro stations for the client and chef
                    string clientMetroQuery = "SELECT ClosestMetro FROM Users WHERE UserID = @UserID";
                    var clientCommand = new MySqlCommand(clientMetroQuery);
                    clientCommand.Parameters.AddWithValue("@UserID", selectedOrder.ClientID);
                    string? clientMetro = _mySQLManager.ExecuteScalar(clientCommand)?.ToString();

                    var chefCommand = new MySqlCommand(clientMetroQuery);
                    chefCommand.Parameters.AddWithValue("@UserID", selectedOrder.ChefID);
                    string? chefMetro = _mySQLManager.ExecuteScalar(chefCommand)?.ToString();

                    if (string.IsNullOrWhiteSpace(clientMetro) || string.IsNullOrWhiteSpace(chefMetro)) {
                        MessageBox.Show("Cannot display route: Client or Chef's closest metro station is not set.");
                        return;
                    }

                    // Step 4: Find the vertices for the client's and chef's metro stations
                    Node<MetroStation>? clientNode = _map.AdjacencyList.Keys
                        .FirstOrDefault(v => v.Object?.LibelleStation == clientMetro);
                    Node<MetroStation>? chefNode = _map.AdjacencyList.Keys
                        .FirstOrDefault(v => v.Object?.LibelleStation == chefMetro);

                    if (clientNode == null || chefNode == null) {
                        MessageBox.Show("Cannot display route: Client or Chef's metro station not found in the map.");
                        return;
                    }

                    // Get all adjacent station names for chefNode
                    var adjacentStationNames = _map.AdjacencyList[chefNode]
                        .Select(tuple => tuple.Item1?.Object?.LibelleStation ?? "null")
                        .ToList();

                    Logger.Log($"chefNode ({chefNode.Object.LibelleStation}) neighbours: {string.Join(", ", adjacentStationNames)}");

                    // Get all adjacent station names for chefNode
                    var adjacentStationNames2 = _map.AdjacencyList[clientNode]
                        .Select(tuple => tuple.Item1?.Object?.LibelleStation ?? "null")
                        .ToList();

                    Logger.Log($"clientNode ({clientNode.Object.LibelleStation}) neighbours: {string.Join(", ", adjacentStationNames2)}");
                    /*// Step 5: Use A* to find the shortest path
                    var aStar = new Astar<MetroStation>();
                    var (path, totalLength) = aStar.Run(_map, chefNode, clientNode);
                    */
                    var dijkstra = new Dijkstra<MetroStation>();
                    dijkstra.Init(_map, chefNode);
                    var (path, totalLength) = dijkstra.GetPath(clientNode);

                    if (path == null || path.Count == 0) {
                        MessageBox.Show($"No path found between the client's ({clientNode.Object.LibelleStation}) and chef's ({chefNode.Object.LibelleStation}) metro stations.");
                        return;
                    }

                    // Step 6: Redraw the map with the path highlighted
                    DrawNodes(path);

                    // Step 7: Switch to the Map tab to show the path
                    if (metroMap.Parent is TabControl tabControl) {
                        tabControl.SelectedItem = metroMap;
                    }
                }
                catch (Exception ex) {
                    Logger.Log($"Error viewing order details: {ex.Message}");
                    MessageBox.Show($"Error viewing order details: {ex.Message}");
                }
            }
        }

        #endregion

        #region SelectionChanged
        public void DgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Logger.Log("Order selection changed");
            if (dgOrders.SelectedItem is Order selectedOrder) {
                Logger.Log($"Selected order: {selectedOrder.OrderID}");
            }
        }

        public void DgDishes_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Logger.Log("Dish selection changed");
            if (dgDishes.SelectedItem is Dish selectedDish) {
                // Could show additional details if needed
                Logger.Log($"Selected dish: {selectedDish.Name}");
            }
        }
        #endregion

        private void UpdateCartTotal() {
            decimal total = _cartItems.Sum(i => i.TotalPrice);
            txtCartTotal.Text = total.ToString("C2");
        }
    }
}