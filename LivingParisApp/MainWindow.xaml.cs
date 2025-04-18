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

        // State Collections
        private Dictionary<int, Button> _dishButtons = new Dictionary<int, Button>();
        // This will allow us to track for dishes which have been added to the cart
        // By getting their id, we can then backtrack and target the source button


        // Observable Collections
        private ObservableCollection<string> _allMetroName = new();
        private ObservableCollection<Dish> _allDishes = new();                  // All Dishes
        private ObservableCollection<Dish> _myDishes = new();                   // Manage My Dishes tab
        private ObservableCollection<Dish> _filteredAvailableDishes = new();    // Browse and Order tab || Marketplace
        private ObservableCollection<Dish> _filteredDishes = new();             // Admin view -> All Dishes but filtered
        private ObservableCollection<CartItem> _cartItems = new();    // Shopping cart
        private ObservableCollection<Order> _allOrders = new();
        private ObservableCollection<Order> _myOrders = new();          // My Orders tab
        private ObservableCollection<Order> _filteredOrders = new();     // Orders tab in admin view
        private ObservableCollection<User> _allUsers = new();
        private ObservableCollection<User> _filteredUsers = new(); // Store all users for filtering

        // filters ObservableCollection
        private ObservableCollection<string> DishTypes;
        private ObservableCollection<string> Diets;
        private ObservableCollection<string> Origins;

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
                    _allMetroName.Add(stationName);
                }
            }

            // Set up event handlers
            btnSignIn.Click += BtnSignIn_Click;
            btnSignUp.Click += BtnSignUp_Click;
            btnSignOut.Click += BtnSignOut_Click;

            // Initialize data bindings
            cmbMetro.ItemsSource = _allMetroName;
            cmbEditClosestMetro.ItemsSource = _allMetroName;

            dgDishes.ItemsSource = _filteredAvailableDishes; // market place
            lbCart.ItemsSource = _cartItems;
            dgMyDishes.ItemsSource = _myDishes;
            dgOrders.ItemsSource = _myOrders;
            dgAdminOrders.ItemsSource = _filteredOrders;
            dgUsers.ItemsSource = _filteredUsers;
            dgAdminDishes.ItemsSource = _filteredDishes;

            // Hide tabs initially
            tabAccount.Visibility = Visibility.Collapsed;
            tabFoodServices.Visibility = Visibility.Collapsed;
            metroMap.Visibility = Visibility.Collapsed;
            adminTab.Visibility = Visibility.Collapsed;

            InitializeMapTransforms();
            LoadInitialData();

            cmbDishType.ItemsSource = DishTypes;
            cmbDiet.ItemsSource = Diets;
            cmbOrigin.ItemsSource = Origins;

            this.Loaded += (sender, e) => DrawNodes();
        }

        private void LoadInitialData() {
            Logger.Log("Loading initial data...");
            LoadAllDishes(); // Marketplace / admin view

            InitializeFiltersData(); // should be before applying filters data to avoid conflicts and missing initialization
            BtnApplyFiltersDishes(); // apply view after loading every dishes from the database
        }

        private void InitializeFiltersData() {
            Diets = new ObservableCollection<string>(_allDishes.Select(d => d.Diet).Distinct());
            Origins = new ObservableCollection<string>(_allDishes.Select(d => d.Origin).Distinct());
            DishTypes = new ObservableCollection<string>(_allDishes.Select(d => d.Type).Distinct());

            Diets.Add("All");
            Origins.Add("All");
            DishTypes.Add("All");

            // Move "All" to the top if needed
            Diets.Move(Diets.Count - 1, 0);
            Origins.Move(Origins.Count - 1, 0);
            DishTypes.Move(DishTypes.Count - 1, 0);
        }

        #region Authentication
        private void AdminAdditionalLoad() {
            LoadAllUsers(); // load all users
            LoadAllOrders(); // load all orders

            BtnSearchUser_Click();
            BtnSearchDish_Click();
            BtnFilterOrders_Click();
        }

        private void UpdateUIForLoggedInUser() {
            // loading current user data
            LoadMyDishes();
            LoadMyOrders();

            tabAccount.Visibility = Visibility.Visible;
            tabSignIn.Visibility = Visibility.Collapsed;
            tabSignUp.Visibility = Visibility.Collapsed;
            tabFoodServices.Visibility = Visibility.Visible;
            metroMap.Visibility = Visibility.Visible;

            adminTab.Visibility = Visibility.Collapsed;
            if (_currentUser.UserID == 1) {
                adminTab.Visibility = Visibility.Visible;
                AdminAdditionalLoad(); //Additional admin loads
            }


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
            adminTab.Visibility = Visibility.Collapsed;

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

        private void txtSignIn_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                BtnSignIn_Click();
            }
        }

        private void BtnSignIn_Click(object sender = null, RoutedEventArgs e = null) {
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

                // Create a content canvas that will be transformed
                Canvas contentCanvas = new Canvas {
                    Width = 1000,
                    Height = 1000,
                    Background = Brushes.Transparent
                };

                // Extract all valid stations with coordinates
                var stations = _map.AdjacencyList.Keys
                    .Where(node => node?.Object != null &&
                          node.Object.Longitude != 0 &&
                          node.Object.Latitude != 0)
                    .ToList();

                if (!stations.Any()) {
                    return;
                }

                // Calculate geographical boundaries
                // SWAP LONGITUDE AND LATITUDE HERE
                double minX = stations.Min(s => s.Object.Latitude);  // Using latitude for X
                double maxX = stations.Max(s => s.Object.Latitude);
                double minY = stations.Min(s => s.Object.Longitude); // Using longitude for Y
                double maxY = stations.Max(s => s.Object.Longitude);

                // Add padding (10%)
                double xPadding = (maxX - minX) * 0.1;
                double yPadding = (maxY - minY) * 0.1;

                minX -= xPadding;
                maxX += xPadding;
                minY -= yPadding;
                maxY += yPadding;

                // Create path edge list for highlighting
                var pathEdges = new List<(Node<MetroStation> Start, Node<MetroStation> End)>();
                if (path != null && path.Count > 1) {
                    var current = path.First;
                    while (current?.Next != null) {
                        pathEdges.Add((current.Value, current.Next.Value));
                        current = current.Next;
                    }
                }

                // Calculate and store coordinates for all stations
                var stationCoordinates = new Dictionary<Node<MetroStation>, Point>();
                foreach (var station in stations) {
                    // SWAP LONGITUDE AND LATITUDE FOR MAPPING
                    // Use latitude for X and longitude for Y
                    double x = ((station.Object.Latitude - minX) / (maxX - minX)) * 1000;
                    double y = 1000 - ((station.Object.Longitude - minY) / (maxY - minY)) * 1000;

                    // Ensure coordinates are within canvas bounds
                    x = Math.Max(10, Math.Min(990, x));
                    y = Math.Max(10, Math.Min(990, y));

                    stationCoordinates[station] = new Point(x, y);
                }

                // Rest of the code remains the same...
                // Draw connections
                foreach (var stationNode in stations) {
                    if (!stationCoordinates.TryGetValue(stationNode, out Point startPoint))
                        continue;

                    // Get all connected stations
                    var connections = _map.AdjacencyList[stationNode];
                    foreach (var connection in connections) {
                        var neighborNode = connection.Item1;
                        if (neighborNode == null || !stationCoordinates.TryGetValue(neighborNode, out Point endPoint))
                            continue;

                        // Determine if this connection is part of the highlighted path
                        bool isPathConnection = pathEdges.Any(edge =>
                            (edge.Start == stationNode && edge.End == neighborNode) ||
                            (edge.Start == neighborNode && edge.End == stationNode));

                        // Get line color
                        string lineCode = stationNode.Object.LibelleLine ?? "default";
                        Brush lineColor = isPathConnection
                            ? Brushes.Yellow
                            : (LineColors.TryGetValue(lineCode, out var color) ? color : Brushes.Gray);

                        // Create the line
                        var line = new Line {
                            X1 = startPoint.X,
                            Y1 = startPoint.Y,
                            X2 = endPoint.X,
                            Y2 = endPoint.Y,
                            Stroke = lineColor,
                            StrokeThickness = isPathConnection ? 5 : 3,
                            StrokeStartLineCap = PenLineCap.Round
                        };

                        Panel.SetZIndex(line, isPathConnection ? 1 : 0);
                        contentCanvas.Children.Add(line);
                    }
                }

                // Draw stations
                foreach (var station in stations) {
                    if (!stationCoordinates.TryGetValue(station, out Point point))
                        continue;

                    bool isPathStation = path != null && path.Contains(station);

                    var circle = new Ellipse {
                        Width = 12,
                        Height = 12,
                        Fill = isPathStation ? Brushes.Yellow : Brushes.White,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Cursor = Cursors.Hand,
                        Tag = station.Object.LibelleStation
                    };

                    Canvas.SetLeft(circle, point.X - 6);
                    Canvas.SetTop(circle, point.Y - 6);
                    Panel.SetZIndex(circle, 2);

                    var label = new TextBlock {
                        Text = station.Object.LibelleStation ?? "Unknown",
                        FontSize = 10,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                        Padding = new Thickness(3),
                        Visibility = Visibility.Hidden
                    };

                    Canvas.SetLeft(label, point.X + 8);
                    Canvas.SetTop(label, point.Y - 8);
                    Panel.SetZIndex(label, 3);

                    circle.MouseEnter += (s, e) => {
                        label.Visibility = Visibility.Visible;
                        ((Ellipse)s).Fill = Brushes.LightYellow;
                    };

                    circle.MouseLeave += (s, e) => {
                        label.Visibility = Visibility.Hidden;
                        ((Ellipse)s).Fill = isPathStation ? Brushes.Yellow : Brushes.White;
                    };

                    contentCanvas.Children.Add(circle);
                    contentCanvas.Children.Add(label);
                }

                // Apply transform and add to main canvas
                contentCanvas.RenderTransform = _transformGroup;
                metroCanvas.Children.Add(contentCanvas);
            }
            catch (Exception ex) {
                MessageBox.Show($"Error drawing map: {ex.Message}", "Drawing Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region my account logic

        private void BtnEditAccount_Click(object sender, RoutedEventArgs e) {
            // Fill edit fields with current values
            txtEditName.Text = txtUserInfo.Text.Replace("Welcome, ", "");
            txtEditEmail.Text = txtUserEmail.Text.Replace("Email: ", "");
            cmbEditClosestMetro.Text = txtClosestMetro.Text.Replace("ClosestMetro: ", "");

            // Set checkboxes to match current roles
            chkEditClient.IsChecked = chkAccountClient.IsChecked;
            chkEditChef.IsChecked = chkAccountChef.IsChecked;

            // Switch to edit mode
            viewModePanel.Visibility = Visibility.Collapsed;
            editModePanel.Visibility = Visibility.Visible;
        }

        private void BtnSaveAccount_Click(object sender, RoutedEventArgs e) {
            // Update user information with edited values
            txtUserInfo.Text = "Welcome, " + txtEditName.Text;
            txtUserEmail.Text = "Email: " + txtEditEmail.Text;
            txtClosestMetro.Text = "ClosestMetro: " + cmbEditClosestMetro.Text;

            // Update role checkboxes
            chkAccountClient.IsChecked = chkEditClient.IsChecked;
            chkAccountChef.IsChecked = chkEditChef.IsChecked;

            // Save changes to database or backend
            SaveUserChangesToDatabase();

            // Display success message
            txtUpdateStatus.Text = "Account details updated successfully!";

            // Switch back to view mode
            viewModePanel.Visibility = Visibility.Visible;
            editModePanel.Visibility = Visibility.Collapsed;
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) {
            // Clear any status messages
            txtUpdateStatus.Text = "";

            // Switch back to view mode without saving changes
            viewModePanel.Visibility = Visibility.Visible;
            editModePanel.Visibility = Visibility.Collapsed;
        }

        // Helper method to save changes to your database
        private void SaveUserChangesToDatabase() {
            // Implement your database update logic here
            // This will depend on how your application stores user data

            // Example:
            // var currentUser = GetCurrentUser();
            // currentUser.Name = txtEditName.Text;
            // currentUser.Email = txtEditEmail.Text;
            // currentUser.ClosestMetro = txtEditClosestMetro.Text;
            // currentUser.IsClient = chkEditClient.IsChecked ?? false;
            // currentUser.IsChef = chkEditChef.IsChecked ?? false;
            // _userRepository.UpdateUser(currentUser);
        }

        #endregion

        #region Loader

        private void LoadMyDishes() {
            /// <summary>
            /// This load dishes method is used to load the chef view mode of all of his dishes
            /// It should only be called ONCE at the start of the program
            /// </summary>
            Logger.Log("Loading all current user's dishes...");
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
                Logger.Log($"Loaded {_myDishes.Count} dishes from current user");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading my dishes: {ex.Message}");
                MessageBox.Show($"Error loading my dishes: {ex.Message}");
            }
        }

        private void LoadMyOrders() {
            /// <summary>
            /// This load orders method is used to load the client view mode of all of his orders
            /// It should only be called ONCE at the start of the program
            /// </summary>
            Logger.Log("Loading all current user's order...");
            try {
                _myOrders.Clear();
                var query = cmbOrderView.SelectedIndex == 0
                    ? @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
                    uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                    uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                FROM Orders o
                JOIN Users uc ON o.ClientID = uc.UserID
                JOIN Users uch ON o.ChefID = uch.UserID
                WHERE o.ClientID = @UserID AND o.Status = 'Pending'"
                    : @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
                    uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                    uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                FROM Orders o
                JOIN Users uc ON o.ClientID = uc.UserID
                JOIN Users uch ON o.ChefID = uch.UserID
                WHERE o.ChefID = @UserID AND o.Status = 'Pending'";

                var command = new MySqlCommand(query);
                command.Parameters.AddWithValue("@UserID", _currentUser?.UserID ?? 0);

                using (var reader = _mySQLManager.ExecuteReader(command)) {
                    while (reader.Read()) {
                        _myOrders.Add(new Order {
                            OrderID = reader.GetInt32("OrderID"),
                            ClientID = reader.GetInt32("ClientID"),
                            ChefID = reader.GetInt32("ChefID"),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString("Address"),
                            OrderDate = reader.GetDateTime("OrderDate"),
                            OrderTotal = reader.GetDecimal("OrderTotal"),
                            Status = reader.GetString("Status"),
                            ClientName = $"{reader.GetString("ClientFirst")} {reader.GetString("ClientLast")}",
                            ChefName = $"{reader.GetString("ChefFirst")} {reader.GetString("ChefLast")}"
                        });
                    }
                }
                Logger.Log($"Loaded {_myOrders.Count} orders from current user");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading orders: {ex.Message}");
                MessageBox.Show($"Error loading orders: {ex.Message}");
            }
        }

        private void LoadAllUsers() {
            /// <summary>
            /// This method is used to load all users within the database
            /// It should only be called ONCE at the start of the program
            /// </summary>
            /// <value></value>
            try {
                _allUsers.Clear();

                var query = @"SELECT * from Users";
                using (var reader = _mySQLManager.ExecuteReader(query)) {
                    while (reader.Read()) {
                        _allUsers.Add(new User {
                            UserID = reader.GetInt32("UserID"),
                            LastName = reader.GetString("LastName"),
                            FirstName = reader.GetString("FirstName"),
                            Street = reader.GetString("Street"),
                            StreetNumber = reader.GetInt32("StreetNumber"),
                            Postcode = reader.GetString("Postcode"),
                            City = reader.GetString("City"),
                            PhoneNumber = reader.GetString("PhoneNumber"),
                            Mail = reader.GetString("Mail"),
                            ClosestMetro = reader.GetString("ClosestMetro"),
                            Password = reader.GetString("Password"),
                            IsClient = reader.GetInt32("IsClient"),
                            IsChef = reader.GetInt32("IsChef"),
                        });
                    }
                }
                Logger.Log($"Loaded {_allUsers.Count} users");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading users: {ex.Message}");
            }
        }

        private void LoadAllOrders() {
            /// <summary>
            /// This method is used to load all orders within the database
            /// It should only be called ONCE at the start of the program
            /// </summary>
            /// <value>
            /// This will return all orders from every client
            /// </value>
            Logger.Log("Loading all orders...");
            try {
                _allOrders.Clear();
                var query = @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
                            uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                            uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                        FROM Orders o
                        JOIN Users uc ON o.ClientID = uc.UserID
                        JOIN Users uch ON o.ChefID = uch.UserID";

                using (var reader = _mySQLManager.ExecuteReader(query)) {
                    while (reader.Read()) {
                        _allOrders.Add(new Order {
                            OrderID = reader.GetInt32("OrderID"),
                            ClientID = reader.GetInt32("ClientID"),
                            ChefID = reader.GetInt32("ChefID"),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString("Address"),
                            OrderDate = reader.GetDateTime("OrderDate"),
                            OrderTotal = reader.GetDecimal("OrderTotal"),
                            Status = reader.GetString("Status"),
                            ClientName = $"{reader.GetString("ClientFirst")} {reader.GetString("ClientLast")}",
                            ChefName = $"{reader.GetString("ChefFirst")} {reader.GetString("ChefLast")}"
                        });
                    }
                }
                Logger.Log($"Loaded {_allOrders.Count} orders");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading orders: {ex.Message}");
            }
        }

        private void LoadAllDishes() {
            /// <summary>
            /// This method is used to load all active dishes within the database
            /// It should only be called ONCE at the start of the program
            /// </summary>
            /// <value>
            /// This will return all dishes created and valid from the chefs
            /// </value>
            Logger.Log("Loading all dishes...");
            try {
                _allDishes.Clear();
                var query = @"SELECT d.*, CONCAT(u.FirstName, ' ', u.LastName) AS ChefName
                            FROM Dishes d
                            JOIN Users u ON u.UserID = d.ChefID
                            WHERE u.IsChef = 1;";
                var command = new MySqlCommand(query);
                using (var reader = _mySQLManager.ExecuteReader(command)) {
                    while (reader.Read()) {
                        _allDishes.Add(new Dish {
                            DishID = reader.GetInt32("DishID"),
                            ChefID = reader.GetInt32("ChefID"),
                            Name = reader.GetString("Name"),
                            Type = reader.GetString("Type"),
                            DishPrice = reader.GetDecimal("DishPrice"),
                            FabricationDate = reader.GetDateTime("FabricationDate"),
                            PeremptionDate = reader.GetDateTime("PeremptionDate"),
                            Diet = reader.GetString("Diet"),
                            Origin = reader.GetString("Origin"),
                            ChefName = reader.GetString("ChefName"),
                        });
                    }
                }
                Logger.Log($"Loaded {_allDishes.Count} dishes");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading dishes: {ex.Message}");
            }
        }
        #endregion

        #region Click

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

                // Step 2: Group cart items by chef
                var chefGroups = _cartItems.GroupBy(item => item.Dish.ChefID);
                List<int> createdOrderIds = new List<int>();

                // Process each chef group as a separate order
                foreach (var chefGroup in chefGroups) {
                    int chefId = chefGroup.Key;
                    var chefItems = chefGroup.ToList();
                    List<CartItem> processedItems = new List<CartItem>();

                    // Calculate the order total for this chef
                    decimal orderTotal = chefItems.Sum(item => item.TotalPrice);

                    // Insert the order into the Orders table
                    string insertOrderQuery = @"
                        INSERT INTO Orders (ClientID, ChefID, Address, OrderDate, OrderTotal, Status)
                        VALUES (@ClientID, @ChefID, @Address, @OrderDate, @OrderTotal, @Status);
                        SELECT LAST_INSERT_ID();";
                    var orderCommand = new MySqlCommand(insertOrderQuery);
                    orderCommand.Parameters.AddWithValue("@ClientID", _currentUser.UserID);
                    orderCommand.Parameters.AddWithValue("@ChefID", chefId);
                    orderCommand.Parameters.AddWithValue("@Address", orderAddress);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@OrderTotal", orderTotal);
                    orderCommand.Parameters.AddWithValue("@Status", "Pending");

                    int orderId = Convert.ToInt32(_mySQLManager.ExecuteScalar(orderCommand));

                    //fetching chef name for order information
                    var chefNameQuery = @"
                        SELECT CONCAT(FirstName, ' ', LastName) AS FullName
                        FROM Users
                        WHERE UserID = @ChefID AND IsChef = 1";
                    var chefNameCommand = new MySqlCommand(chefNameQuery);
                    chefNameCommand.Parameters.AddWithValue("@ChefID", chefId);

                    var chefName = _mySQLManager.ExecuteScalar(chefNameCommand);

                    Order newOrder = new Order() {
                        OrderID = orderId,
                        ClientID = _currentUser.UserID,
                        ChefID = chefId,
                        Address = orderAddress,
                        OrderDate = DateTime.Now,
                        OrderTotal = orderTotal,
                        Status = "Pending",
                        ClientName = $"{_currentUser.FirstName} {_currentUser.LastName}",
                        ChefName = (string)chefName
                    };
                    // Adding new order to the observable collections
                    _allOrders.Add(newOrder);
                    _myOrders.Add(newOrder);
                    _filteredOrders.Add(newOrder);
                    createdOrderIds.Add(orderId);

                    Logger.Log($"Created order with OrderID: {orderId} for ChefID: {chefId}");

                    // Insert each cart item into the OrderDishes table
                    foreach (var cartItem in chefItems) {
                        string insertOrderDishQuery = @"
                            INSERT INTO OrderDishes (OrderID, DishID, Quantity)
                            VALUES (@OrderID, @DishID, @Quantity)";
                        var orderDishCommand = new MySqlCommand(insertOrderDishQuery);
                        orderDishCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderDishCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);
                        orderDishCommand.Parameters.AddWithValue("@Quantity", cartItem.Quantity);

                        _mySQLManager.ExecuteNonQuery(orderDishCommand);
                        Logger.Log($"Added dish {cartItem.Dish.DishID} to OrderDishes with quantity {cartItem.Quantity}");

                        processedItems.Add(cartItem);
                    }

                    // removing dish from the market place
                    foreach (var cartItem in processedItems) {
                        string deleteDishQuery = "DELETE FROM Dishes WHERE DishID = @DishID";
                        var deleteCommand = new MySqlCommand(deleteDishQuery);
                        deleteCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);

                        _mySQLManager.ExecuteNonQuery(deleteCommand);
                        Logger.Log($"Removed dish {cartItem.Dish.DishID} from Dishes table");

                        _allDishes.Remove(cartItem.Dish);
                        _filteredAvailableDishes.Remove(cartItem.Dish);
                    }
                }

                // Update the UI
                _cartItems.Clear();
                UpdateCartTotal();

                // Show success message with order count information
                if (createdOrderIds.Count == 1) {
                    MessageBox.Show($"Order #{createdOrderIds[0]} placed successfully!");
                }
                else {
                    MessageBox.Show($"{createdOrderIds.Count} orders placed successfully! Order IDs: {string.Join(", ", createdOrderIds)}");
                }
            }
            catch (Exception ex) {
                Logger.Log($"Error placing order: {ex.Message}");
                MessageBox.Show($"Error placing order: {ex.Message}");
            }
        }

        public void BtnCancelOrder_Click(object sender, EventArgs e) {
            try {
                var button = sender as Button;
                if (button?.Tag is int orderId) {
                    var order = dgOrders.Items.OfType<Order>().FirstOrDefault(o => o.OrderID == orderId);
                    if (order != null) {
                        var query = $@"UPDATE Orders
                                    SET Status = 'Cancelled'
                                    WHERE {orderId} = OrderID";
                        var command = new MySqlCommand(query);
                        _mySQLManager.ExecuteNonQuery(command);
                        _myOrders.Remove(order); // remove the order from the view of the client

                        var orderToCancelAllOrder = _allOrders.FirstOrDefault(d => d.OrderID == orderId);
                        if (orderToCancelAllOrder != null) {
                            orderToCancelAllOrder.Status = "Cancelled";
                        }

                        var orderToCancelFilteredOrders = _filteredOrders.FirstOrDefault(d => d.OrderID == orderId);
                        if (orderToCancelFilteredOrders != null) {
                            orderToCancelFilteredOrders.Status = "Cancelled";
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void BtnAddToCart_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Add to cart button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                _dishButtons[dishId] = button;

                var dish = _allDishes.FirstOrDefault(d => d.DishID == dishId);
                if (dish != null) {
                    var existingItem = _cartItems.FirstOrDefault(i => i.Dish.DishID == dishId);
                    if (existingItem != null) {
                        MessageBox.Show("This dish is already in your cart.", "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else {
                        _cartItems.Add(new CartItem { Dish = dish, Quantity = 1 });
                    }
                    UpdateCartTotal();

                    // Disable the add button for this dish
                    button.IsEnabled = false;
                    button.Content = "In Cart";
                }
            }
        }

        public void RemoveFromCart_Click(object sender, RoutedEventArgs e) {
            // Get the cart item that needs to be removed
            var cartItem = (sender as Button).CommandParameter as CartItem;

            // Remove it from your cart collection (assuming you have an ObservableCollection)
            var cartItems = lbCart.ItemsSource as ObservableCollection<CartItem>;
            if (cartItems != null && cartItem != null) {
                cartItems.Remove(cartItem);

                // If you're tracking total price elsewhere, update it
                UpdateCartTotal();

                // Reset the button if we have a reference to it
                if (_dishButtons.TryGetValue(cartItem.Dish.DishID, out Button addButton)) {
                    addButton.IsEnabled = true;
                    addButton.Content = "+";
                    _dishButtons.Remove(cartItem.Dish.DishID);
                }
            }
        }

        public void BtnAddNewDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("adding new dish");
            var addWindow = new AddNewDishWindow(_mySQLManager, _currentUser);
            if (addWindow.ShowDialog() == true) {
                Dish dish = addWindow.CreatedDish;
                AddDishInCollections(dish);
                // if we are only creating a new dish, add it to the collections
            }
        }

        public void BtnEditDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Edit dish button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                var dish = _myDishes.FirstOrDefault(d => d.DishID == dishId);
                if (dish != null) {
                    var editWindow = new AddNewDishWindow(_mySQLManager, _currentUser, dish);
                    editWindow.Owner = this; // Set the owner to the current window
                    bool? result = editWindow.ShowDialog(); // Actually show the dialog
                    // we already have implemented changed propretiy notifications, so there is nothing to do here anymore
                    // any update has already been done in the AddNewDish Window.
                }
            }
        }

        public void BtnDeleteDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Delete dish button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                if (MessageBox.Show("Are you sure you want to delete this dish?",
                    "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    try {
                        Logger.Log("Deleting the targeted dish...");
                        string query = "DELETE FROM Dishes WHERE DishID = @DishID";
                        var command = new MySqlCommand(query);
                        command.Parameters.AddWithValue("@DishID", dishId);
                        _mySQLManager.ExecuteNonQuery(command);

                        RemoveDishFromCollections(dishId); // removing dish from all observable collections

                        Logger.Log("Deleted the targeted dish.");
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

        public void BtnApplyFiltersDishes(object sender = null, RoutedEventArgs e = null) {
            _filteredAvailableDishes.Clear();

            // Start with full set
            IEnumerable<Dish> filteredAvailableDishes = _allDishes;
            bool anyFilterApplied = false;

            // Apply Type filter if not "All"
            if (!string.IsNullOrEmpty(cmbDishType.Text) && cmbDishType.Text != "All") {
                filteredAvailableDishes = filteredAvailableDishes.Where(d => d.Type == cmbDishType.Text);
                anyFilterApplied = true;
            }

            // Apply Diet filter if not "All"
            if (!string.IsNullOrEmpty(cmbDiet.Text) && cmbDiet.Text != "All") {
                filteredAvailableDishes = filteredAvailableDishes.Where(d => d.Diet == cmbDiet.Text);
                anyFilterApplied = true;
            }

            // Apply Origin filter if not "All"
            if (!string.IsNullOrEmpty(cmbOrigin.Text) && cmbOrigin.Text != "All") {
                filteredAvailableDishes = filteredAvailableDishes.Where(d => d.Origin == cmbOrigin.Text);
                anyFilterApplied = true;
            }

            // If no filters were applied, show all dishes
            if (!anyFilterApplied) {
                filteredAvailableDishes = _allDishes;
            }

            // Remove duplicates that might have matched multiple criteria
            filteredAvailableDishes = filteredAvailableDishes.Distinct();

            foreach (Dish dish in filteredAvailableDishes) {
                _filteredAvailableDishes.Add(dish);
            }

            // Show message if no results found
            if (!_filteredAvailableDishes.Any()) {
                MessageBox.Show("No dishes found matching the search criteria.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Admin
        // Admin Tab - User Management
        private void TxtSearchUser_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                // Trigger the search button's click event
                BtnSearchUser_Click(sender, e);
            }
        }

        private void BtnSearchUser_Click(object sender = null, RoutedEventArgs e = null) {
            string searchEmail = txtSearchUser.Text.Trim().ToLower();
            _filteredUsers.Clear();

            IEnumerable<User> filteredUsers;

            if (string.IsNullOrEmpty(searchEmail)) {
                // If search is empty, show all users sorted by email
                filteredUsers = _allUsers;
            }
            else {
                // More flexible matching approach
                filteredUsers = _allUsers
                    .Where(user => {
                        string fullEmail = user.Mail.ToLower();
                        string username = fullEmail.Split('@')[0]; // Extract username part

                        // Match if:
                        // 1. Email contains the search term as substring (most intuitive)
                        // 2. OR Levenshtein distance is small enough (for typo tolerance)
                        return fullEmail.Contains(searchEmail) ||
                               username.Contains(searchEmail) ||
                               CalculateLevenshteinDistance(searchEmail, fullEmail) <= 3 ||
                               CalculateLevenshteinDistance(searchEmail, username) <= 2;
                    })
                    .OrderBy(x => x.Mail);
            }

            // Update ObservableCollection
            foreach (var user in filteredUsers) {
                _filteredUsers.Add(user);
            }

            // Show message if no results found
            if (!_filteredUsers.Any()) {
                MessageBox.Show("No users found matching the search criteria.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag != null) {
                string userId = button.Tag.ToString();
                // Implement edit user logic
                // Possibly open a dialog with user details for editing
                User userToEdit = _allUsers.FirstOrDefault(u => u.UserID.ToString() == userId);

                if (userToEdit != null) {
                    var editWindow = new EditUserWindow(_mySQLManager, userToEdit);
                    editWindow.Owner = this; // Set the owner to keep window management clean

                    if (editWindow.ShowDialog() == true) {
                        // User was updated, refresh your UI if needed
                        // For example, if this is a list, refresh the list
                        // usersListView.Items.Refresh();

                        MessageBox.Show($"User {userToEdit.FullName} was updated successfully",
                                      "Success",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }
                else {
                    MessageBox.Show("User not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is int userId) {
                if (MessageBox.Show("Are you sure you want to delete this user?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    // Proceed with deletion
                    try {
                        Logger.Log("Deleting the targeted user...");
                        string query = "DELETE FROM Users WHERE UserID = @UserID";
                        var command = new MySqlCommand(query);
                        command.Parameters.AddWithValue("@UserID", userId);
                        _mySQLManager.ExecuteNonQuery(command);

                        RemoveUserFromCollections(userId); // removing user from all observable collections

                        Logger.Log("Deleted the targeted user.");
                    }
                    catch (Exception ex) {
                        Logger.Log($"Error deleting user: {ex.Message}");
                        MessageBox.Show($"Error deleting user: {ex.Message}");
                    }
                }
            }
        }

        // Admin Tab - Dish Management
        private void TxtSearchDish_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                // Trigger the search button's click event
                BtnSearchDish_Click(sender, e);
            }
        }

        private void BtnSearchDish_Click(object sender = null, RoutedEventArgs e = null) {
            string searchDish = txtSearchDish.Text.Trim().ToLower();
            _filteredDishes.Clear();

            IEnumerable<Dish> filteredDishes;

            if (string.IsNullOrEmpty(searchDish)) {
                // If search is empty, show all users sorted by email
                filteredDishes = _allDishes;
            }
            else {
                // More flexible matching approach
                filteredDishes = _allDishes
                    .Where(dish => {
                        string dishName = dish.Name.ToLower();

                        // Match if:
                        // 1. Dish name contains the search term (most intuitive)
                        // 2. OR Levenshtein distance is small enough for typo tolerance
                        return dishName.Contains(searchDish) ||
                            CalculateLevenshteinDistance(searchDish, dishName) <= Math.Min(3, searchDish.Length);
                    })
                    .OrderBy(dish => dish.Name.ToLower().Contains(searchDish) ? 0 : 1) // Exact substring matches first
                    .ThenBy(dish => dish.Name); // Then alphabetically
            }

            // Update ObservableCollection
            foreach (var dish in filteredDishes) {
                _filteredDishes.Add(dish);
            }

            // Show message if no results found
            if (!filteredDishes.Any()) {
                MessageBox.Show("No dishes found matching the search criteria.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditAdminDish_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Edit dish Admin button clicked");
            if (sender is Button button && button.Tag is int dishId) {
                Logger.Log(dishId);
                Logger.Log(button);
                var dish = _allDishes.FirstOrDefault(d => d.DishID == dishId); // this line differs from the other edit, as the collection targeted is different
                if (dish != null) {
                    var editWindow = new AddNewDishWindow(_mySQLManager, _currentUser, dish);
                    editWindow.Owner = this; // Set the owner to the current window
                    bool? result = editWindow.ShowDialog(); // Actually show the dialog
                    // we already have implemented changed propretiy notifications, so there is nothing to do here anymore
                    // any update has already been done in the AddNewDish Window.
                }
                else {
                    Logger.Log(string.Join(", ", _filteredDishes.Select(d => d.DishID)));
                }
            }
        }

        private void BtnDeleteAdminDish_Click(object sender, RoutedEventArgs e) {
            BtnDeleteDish_Click(sender, e); //reusing user delete dish method, which already target the right collection
        }

        // Admin Tab - Order Management
        private void BtnFilterOrders_Click(object sender = null, RoutedEventArgs e = null) {
            // Implement filter orders logic
            string selectedStatus = (cmbOrderStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
            Logger.Log($"Order filter status in admin dashboard : {selectedStatus}");

            _filteredOrders.Clear();

            IEnumerable<Order> filteredOrders;

            if (string.IsNullOrEmpty(selectedStatus) || selectedStatus == "All") {
                // If search is empty, show all users sorted by order
                filteredOrders = _allOrders;
            }
            else {
                // Fuzzy match using Levenshtein distance
                filteredOrders = _allOrders
                    .Select(order => new {
                        Order = order,
                        Distance = CalculateLevenshteinDistance(selectedStatus, order.Status.ToLower())
                    })
                    .Where(x => x.Distance <= Math.Max(3, selectedStatus.Length / 2)) // Adjust threshold as needed
                    .OrderBy(x => x.Distance) // Closest matches first
                    .ThenBy(x => x.Order.Status) // Then sort by Order name
                    .Select(x => x.Order);
            }

            // Update ObservableCollection
            foreach (var order in filteredOrders) {
                _filteredOrders.Add(order);
            }

            // Show message if no results found
            if (!filteredOrders.Any()) {
                MessageBox.Show("No orders found matching the search criteria.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else {
                Logger.Log($"Loaded {_filteredOrders.Count} orders after filter");
            }
        }

        private void BtnViewAdminOrder_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag != null) {
                string orderId = button.Tag.ToString();
                // Implement view order details logic
                // Possibly open a dialog showing order details
                MessageBox.Show("Not Implemented Yet");
            }
        }
        #endregion

        #region Calculations
        private int CalculateLevenshteinDistance(string a, string b) {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 0;
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] matrix = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++) {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }

            return matrix[a.Length, b.Length];
        }
        #endregion

        #region Actions

        private void RemoveUserFromCollections(int userId) {
            var userToRemoveAllUsers = _allUsers.FirstOrDefault(d => d.UserID == userId);
            if (userToRemoveAllUsers != null) {
                _allUsers.Remove(userToRemoveAllUsers);
            }
            var userToRemoveFilteredUsers = _filteredUsers.FirstOrDefault(d => d.UserID == userId);
            if (userToRemoveFilteredUsers != null) {
                _filteredUsers.Remove(userToRemoveFilteredUsers);
            }
        }

        private void RemoveDishFromCollections(int dishId) {
            // Remove the dish from _allDishes
            var dishToRemoveAllDishes = _allDishes.FirstOrDefault(d => d.DishID == dishId);
            if (dishToRemoveAllDishes != null) {
                _allDishes.Remove(dishToRemoveAllDishes);
            }

            // Remove the dish from _myDishes
            var dishToRemoveMyDishes = _myDishes.FirstOrDefault(d => d.DishID == dishId);
            if (dishToRemoveMyDishes != null) {
                _myDishes.Remove(dishToRemoveMyDishes);
            }

            // Remove the dish from _filteredDishes // admin view
            var dishToRemoveFilteredDishes = _filteredDishes.FirstOrDefault(d => d.DishID == dishId);
            if (dishToRemoveFilteredDishes != null) {
                _filteredDishes.Remove(dishToRemoveFilteredDishes);
            }

            // Remove the dish from _filteredDishes // admin view
            var dishToRemoveFilteredAvailableDishes = _filteredAvailableDishes.FirstOrDefault(d => d.DishID == dishId);
            if (dishToRemoveFilteredAvailableDishes != null) {
                _filteredAvailableDishes.Remove(dishToRemoveFilteredAvailableDishes);
            }
        }

        private void AddDishInCollections(Dish dish) {
            _myDishes.Add(dish);
            _allDishes.Add(dish);
            _filteredDishes.Add(dish);
            _filteredAvailableDishes.Add(dish);
        }

        private void UpdateCartTotal() {
            decimal total = _cartItems.Sum(i => i.TotalPrice);
            txtCartTotal.Text = total.ToString("C2");
        }

        #endregion
    }
}