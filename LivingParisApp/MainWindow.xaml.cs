using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using LivingParisApp.Core.GraphStructure;
using LivingParisApp.Services.Logging;
using LivingParisApp.Core.Entities.Models;
using MySql.Data.MySqlClient;
using LivingParisApp.Services.MySQL;
using System.Data;
using LivingParisApp.Core.Mapping;
using System.Collections.ObjectModel;
using LivingParisApp.Core.Engines.ShortestPaths;
using LivingParisApp.Core.Entities.Station;
using System.Windows.Media.Effects;
using LivingParisApp.Core.Engines.GraphColoration;
using System.IO;
using static LivingParisApp.Services.Environment.Constants;
using MapControl;

namespace LivingParisApp {
    public partial class MainWindow : Window {
        MySQLManager _mySQLManager;
        private User _currentUser;
        Map<MetroStation> _map;
        private Dictionary<string, MapItemsControl> _metroLineLayers = new Dictionary<string, MapItemsControl>();
        private Dictionary<string, MapItemsControl> _highlightedMetroLineLayers = new Dictionary<string, MapItemsControl>();
        RelationshipMap<int> relationshipMap;
        Dictionary<int, Node<int>> nodeCache;
        private double minLat, maxLat, minLon, maxLon;

        private Point _lastMousePosition;

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
        private ObservableCollection<Order> _placedOrders = new();          // My Orders tab
        private ObservableCollection<Order> _recievedOrders = new();          // My Orders tab
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

            dgDishes.ItemsSource = _filteredAvailableDishes; // market place
            lbCart.ItemsSource = _cartItems;
            dgMyDishes.ItemsSource = _myDishes;
            dgOrdersPlaced.ItemsSource = _placedOrders;
            dgOrdersReceived.ItemsSource = _recievedOrders;
            dgAdminOrders.ItemsSource = _filteredOrders;
            dgUsers.ItemsSource = _filteredUsers;
            dgAdminDishes.ItemsSource = _filteredDishes; // admin view of all dishes

            // Hide tabs initially
            tabAccount.Visibility = Visibility.Collapsed;
            tabFoodServices.Visibility = Visibility.Collapsed;
            metroMap.Visibility = Visibility.Collapsed;
            adminTab.Visibility = Visibility.Collapsed;
            relationMap.Visibility = Visibility.Collapsed;

            LoadInitialData();

            cmbDishType.ItemsSource = DishTypes;
            cmbDiet.ItemsSource = Diets;
            cmbOrigin.ItemsSource = Origins;

            this.Loaded += (sender, e) => MetroMap_Load();
            this.Loaded += (sender, e) => DrawRelationshipMap();
        }

        private void LoadInitialData() {
            Logger.Log("Loading initial data...");
            LoadAllDishes(); // Marketplace / admin view

            InitializeFiltersData(); // should be before applying filters data to avoid conflicts and missing initialization
            BtnApplyFiltersDishes_Click(); // apply view after loading every dishes from the database
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

            relationMap.Visibility = Visibility.Visible;
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
            if (_currentUser.IsAdmin == 1) {
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
                // Select the account tab
                tabControl.SelectedIndex = 2;
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
            relationMap.Visibility = Visibility.Collapsed;

            // Clear sign in fields
            txtSignInEmail.Text = string.Empty;
            pwdSignIn.Password = string.Empty;
            txtSignInStatus.Text = string.Empty;

            if (tabAccount.Parent is TabControl tabControl) {
                // Select the sign in tab
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
                           p.Postcode, p.City, p.PhoneNumber, p.ClosestMetro, p.TotalMoneySpent, p.IsClient, p.IsChef, p.IsAdmin
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
                            TotalMoneySpent = (decimal)userReader["TotalMoneySpent"],
                            ClosestMetro = (string)userReader["ClosestMetro"],
                            IsChef = (int)userReader["IsChef"],
                            IsClient = (int)userReader["IsClient"],
                            IsAdmin = (int)userReader["IsAdmin"]
                        };
                        UpdateUIForLoggedInUser();
                        // Switch to the Sign In tab (assuming TabControl is the main control)
                        // Get the parent TabControl
                        if (tabAccount.Parent is TabControl tabControl) {
                            // Select the first tab (Sign In tab)
                            tabControl.SelectedIndex = 4;
                        }
                    }
                }

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

            // Basic validation for required fields with specific error messages
            if (string.IsNullOrWhiteSpace(txtFirstName.Text)) {
                txtSignUpStatus.Text = "First name is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLastName.Text)) {
                txtSignUpStatus.Text = "Last name is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtEmail.Text)) {
                txtSignUpStatus.Text = "Email address is required";
                return;
            }
            else if (!IsValidEmail(txtEmail.Text)) {
                txtSignUpStatus.Text = "Please enter a valid email address";
                return;
            }
            if (string.IsNullOrWhiteSpace(pwdSignUp.Password)) {
                txtSignUpStatus.Text = "Password is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPhone.Text)) {
                txtSignUpStatus.Text = "Phone number is required";
                return;
            }
            else if (txtPhone.Text.Length != 10 || !txtPhone.Text.All(char.IsDigit)) {
                txtSignUpStatus.Text = "Phone number must be exactly 10 digits";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtStreet.Text)) {
                txtSignUpStatus.Text = "Street name is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtStreetNumber.Text)) {
                txtSignUpStatus.Text = "Street number is required";
                return;
            }
            else if (!int.TryParse(txtStreetNumber.Text, out _)) {
                txtSignUpStatus.Text = "Street number must be a valid number";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPostcode.Text)) {
                txtSignUpStatus.Text = "Postal code is required";
                return;
            }
            else if (txtPostcode.Text.Length != 5 || !txtPostcode.Text.All(char.IsDigit)) {
                txtSignUpStatus.Text = "Postal code must be exactly 5 digits";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtCity.Text)) {
                txtSignUpStatus.Text = "City is required";
                return;
            }
            if (string.IsNullOrWhiteSpace(cmbMetro.Text)) {
                txtSignUpStatus.Text = "Closest metro station is required";
                return;
            }
            if (!chkClient.IsChecked == true && !chkChef.IsChecked == true) {
                txtSignUpStatus.Text = "Please select at least one role (Client or Chef)";
                return;
            }

            // Check if email already exists
            if (EmailExists(txtEmail.Text.Trim())) {
                txtSignUpStatus.Text = "Email already in use. Please use a different email.";
                return;
            }

            //reset every boxes
            SaveAccountToDatabase();

            ResetSignUpBoxes();
        }

        private void ResetSignUpBoxes() {
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtEmail.Text = "";
            pwdSignUp.Password = "";
            pwdConfirm.Password = "";
            txtPhone.Text = "";
            txtStreet.Text = "";
            txtStreetNumber.Text = "";
            txtPostcode.Text = "";
            txtCity.Text = "";
            cmbMetro.Text = "";
            chkClient.IsChecked = false;
            chkChef.IsChecked = false;
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

        private bool IsValidEmail(string email) {
            try {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch {
                return false;
            }
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e) {
            // Clear current user
            _currentUser = null;

            // Update UI for logged out state
            UpdateUIForLoggedOutUser();
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
                command.Parameters.AddWithValue("@StreetNumber", txtStreetNumber.Text);
                command.Parameters.AddWithValue("@Postcode", txtPostcode.Text);
                command.Parameters.AddWithValue("@City", txtCity.Text);
                command.Parameters.AddWithValue("@PhoneNumber", txtPhone.Text);
                command.Parameters.AddWithValue("@Mail", txtEmail.Text);
                command.Parameters.AddWithValue("@ClosestMetro", cmbMetro.Text);
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

        #region Metro Map

        private void MetroMap_Load() {
            try {
                ParisMap.Center = new Location(48.8566, 2.3522); // Paris center
                ParisMap.ZoomLevel = 13;

                DrawEdges();

                foreach (Node<MetroStation> metroStation in _map.THashSet) {
                    // IMPORTANT: Create a MapItem instead of a Pushpin
                    var mapItem = new MapItem();
                    mapItem.Location = new Location(metroStation.Object.Latitude, metroStation.Object.Longitude);

                    var dataObject = new { Name = metroStation.Object.LibelleStation, Location = new Location(metroStation.Object.Latitude, metroStation.Object.Longitude) };
                    mapItem.DataContext = dataObject;
                    // Apply the style - this will now work since the style targets MapItem
                    mapItem.Style = (Style)FindResource("PushpinItemStyle");

                    // Add the MapItem to the map
                    ParisMap.Children.Add(mapItem);
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }

        // Helper method to get color based on metro line
        private Color GetStationColor(string lineCode) {
            if (string.IsNullOrEmpty(lineCode)) return Colors.RosyBrown;

            // Create a color mapping for different metro lines
            // Create a color mapping for different metro lines
            var colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase) {
                { "1", Colors.DarkBlue },
                { "2", Colors.Red },
                { "3", Colors.Green },
                { "3bis", Colors.OliveDrab },
                { "4", Colors.Purple },
                { "5", Colors.Orange },
                { "6", Colors.Teal },
                { "7", Colors.Aquamarine },
                { "7bis", Colors.MediumVioletRed },
                { "8", Colors.YellowGreen },
                { "9", Colors.Brown },
                { "10", Colors.DeepSkyBlue },
                { "11", Colors.Gold },
                { "12", Colors.Indigo },
                { "13", Colors.Crimson },
                { "14", Colors.LimeGreen },
                // Add more lines as needed
            };

            return colorMap.TryGetValue(lineCode, out var color) ? color : Colors.RosyBrown;
        }

        private void DrawEdges() {
            try {
                foreach (MetroStationLink<MetroStation> link in _map.GetAllLinks()) {
                    // Create a MapPolyline instead of a regular Polyline
                    var start = link.A.Object;
                    var end = link.B.Object;
                    DrawEdge(start, end);
                }
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void DrawHighlightedEdges(LinkedList<Node<MetroStation>> path) {
            // Create path edge list for highlighting
            if (path != null && path.Count > 1) {
                var current = path.First;
                while (current?.Next != null) {
                    DrawEdge(current.Value.Object, current.Next.Value.Object, true);
                    current = current.Next;
                }
            }
        }

        private void DrawEdge(MetroStation start, MetroStation end, bool isHighlighted = false) {
            // Create a MapPolyline instead of a regular Polyline
            var mapPolyline = new MapPolyline();
            Color baseColor = GetStationColor(start.LibelleLine);

            // Create a LocationCollection for the points
            var locations = new LocationCollection {
                new Location(start.Latitude, start.Longitude),
                new Location(end.Latitude, end.Longitude)
            };

            // Set the locations to the polyline
            mapPolyline.Locations = locations;

            // Style the mapPolyline
            if (isHighlighted) {
                mapPolyline.Stroke = new SolidColorBrush(Colors.DarkGoldenrod);
                mapPolyline.StrokeThickness = 10;
                mapPolyline.Opacity = 1;
            }   
            else {
                mapPolyline.Stroke = new SolidColorBrush(baseColor);
                mapPolyline.StrokeThickness = 3;
                mapPolyline.Opacity = 0.7;
            }

            // Add the polyline to the map
            ParisMap.Children.Add(mapPolyline);
        }

        #endregion

        #region Relation Map Graph Coloration
        private void DrawRelationshipMap() {
            // Clear any existing elements
            relationCanvas.Children.Clear();

            // Get the relationship map between clients and chefs
            relationshipMap = GetClientChefRelationships();

            // Apply Welsh-Powell coloring algorithm
            var welshPowell = new WelshPowell<int>(relationshipMap);
            var colorAssignment = welshPowell.ColorGraph();
            int colorCount = welshPowell.GetColorCount();

            // Define colors for nodes based on the coloration result
            var colorBrushes = new List<SolidColorBrush> {
                new SolidColorBrush(Colors.Red),
                new SolidColorBrush(Colors.Blue),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(Colors.Orange),
                new SolidColorBrush(Colors.Purple),
                new SolidColorBrush(Colors.Cyan),
                new SolidColorBrush(Colors.Magenta),
                new SolidColorBrush(Colors.Yellow),
                new SolidColorBrush(Colors.LimeGreen),
                new SolidColorBrush(Colors.Brown)
            };

            // Ensure we have enough colors
            while (colorBrushes.Count < colorCount) {
                // Generate additional colors if needed
                var rnd = new Random();
                colorBrushes.Add(new SolidColorBrush(Color.FromRgb(
                    (byte)rnd.Next(100, 255),
                    (byte)rnd.Next(100, 255),
                    (byte)rnd.Next(100, 255))));
            }

            // Calculate positions for nodes using a force-directed layout algorithm
            Dictionary<Node<int>, Point> nodePositions = CalculateNodePositions(relationshipMap.AdjacencyList);

            // Draw edges first (so they appear behind nodes)
            foreach (var node in relationshipMap.AdjacencyList.Keys) {
                foreach (var adjacentNodeTuple in relationshipMap.AdjacencyList[node]) {
                    var adjacentNode = adjacentNodeTuple.Item1;

                    // Only draw each edge once (since we have bidirectional edges)
                    if (node.Object < adjacentNode.Object) {
                        var line = new Line {
                            X1 = nodePositions[node].X,
                            Y1 = nodePositions[node].Y,
                            X2 = nodePositions[adjacentNode].X,
                            Y2 = nodePositions[adjacentNode].Y,
                            Stroke = new SolidColorBrush(Colors.Gray),
                            StrokeThickness = 1.5
                        };

                        relationCanvas.Children.Add(line);
                    }
                }
            }

            // Draw nodes (clients and chefs)
            foreach (var node in relationshipMap.AdjacencyList.Keys) {
                // Create ellipse for the node
                var nodeColor = colorAssignment.ContainsKey(node) ? colorBrushes[colorAssignment[node]] : new SolidColorBrush(Colors.Gray);

                var ellipse = new Ellipse {
                    Width = 30,
                    Height = 30,
                    Fill = nodeColor,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                };

                // Position the ellipse
                Canvas.SetLeft(ellipse, nodePositions[node].X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, nodePositions[node].Y - ellipse.Height / 2);

                // Add node label (ID)
                var textBlock = new TextBlock {
                    Text = node.Object.ToString(),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                // Center the text in the node
                Canvas.SetLeft(textBlock, nodePositions[node].X - textBlock.ActualWidth / 2);
                Canvas.SetTop(textBlock, nodePositions[node].Y - textBlock.ActualHeight / 2);

                var tooltip = new ToolTip { Content = $"ID: {node.Object}" };
                ToolTipService.SetToolTip(ellipse, tooltip);

                // Add elements to canvas
                relationCanvas.Children.Add(ellipse);
                relationCanvas.Children.Add(textBlock);
            }

            // Add legend for colors
            DrawLegend(colorCount, colorBrushes);
        }

        // Force-directed layout algorithm to position nodes
        private Dictionary<Node<int>, Point> CalculateNodePositions(Dictionary<Node<int>, List<Tuple<Node<int>, double>>> adjacencyList) {
            var nodePositions = new Dictionary<Node<int>, Point>();

            // Initialize random positions
            var random = new Random();
            foreach (var node in adjacencyList.Keys) {
                nodePositions[node] = new Point(
                    random.Next(50, (int)relationCanvas.Width - 50),
                    random.Next(50, (int)relationCanvas.Height - 50)
                );
            }

            // Apply force-directed algorithm (Fruchterman-Reingold)
            const double k = 50.0; // Optimal distance
            const double iterations = 100;
            const double temperature = 0.1; // Controls movement

            for (int i = 0; i < iterations; i++) {
                // Calculate repulsive forces between all nodes
                var displacements = new Dictionary<Node<int>, Vector>();
                foreach (var node in adjacencyList.Keys) {
                    displacements[node] = new Vector(0, 0);
                }

                foreach (var node1 in adjacencyList.Keys) {
                    foreach (var node2 in adjacencyList.Keys) {
                        if (!node1.Equals(node2)) {
                            var pos1 = nodePositions[node1];
                            var pos2 = nodePositions[node2];

                            // Calculate repulsive force
                            var delta = new Vector(pos1.X - pos2.X, pos1.Y - pos2.Y);
                            double distance = Math.Max(0.1, Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y));

                            if (distance > 0) {
                                double repulsiveForce = k * k / distance;
                                displacements[node1] += (delta / distance) * repulsiveForce;
                            }
                        }
                    }
                }

                // Calculate attractive forces between connected nodes
                foreach (var node in adjacencyList.Keys) {
                    foreach (var adjacent in adjacencyList[node]) {
                        var adjacentNode = adjacent.Item1;

                        var pos1 = nodePositions[node];
                        var pos2 = nodePositions[adjacentNode];

                        var delta = new Vector(pos1.X - pos2.X, pos1.Y - pos2.Y);
                        double distance = Math.Max(0.1, Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y));

                        if (distance > 0) {
                            double attractiveForce = distance * distance / k;
                            displacements[node] -= (delta / distance) * attractiveForce;
                            displacements[adjacentNode] += (delta / distance) * attractiveForce;
                        }
                    }
                }

                // Apply displacements with temperature (cooling)
                double coolingFactor = 1.0 - (i / iterations);

                foreach (var node in adjacencyList.Keys) {
                    var displacement = displacements[node];
                    double magnitude = Math.Sqrt(displacement.X * displacement.X + displacement.Y * displacement.Y);

                    if (magnitude > 0) {
                        displacement = displacement / magnitude * Math.Min(magnitude, temperature * coolingFactor);

                        var pos = nodePositions[node];
                        pos.X += displacement.X;
                        pos.Y += displacement.Y;

                        // Keep nodes within canvas bounds
                        pos.X = Math.Max(30, Math.Min(relationCanvas.Width - 30, pos.X));
                        pos.Y = Math.Max(30, Math.Min(relationCanvas.Height - 30, pos.Y));

                        nodePositions[node] = pos;
                    }
                }
            }

            return nodePositions;
        }

        // Draw a color legend
        private void DrawLegend(int colorCount, List<SolidColorBrush> colorBrushes) {
            double startX = relationCanvas.Width - 150;
            double startY = 20;

            // Add legend title
            var legendTitle = new TextBlock {
                Text = "Graph Coloration",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(legendTitle, startX);
            Canvas.SetTop(legendTitle, startY);
            relationCanvas.Children.Add(legendTitle);

            // Add color samples
            for (int i = 0; i < colorCount; i++) {
                var colorRect = new Rectangle {
                    Width = 20,
                    Height = 20,
                    Fill = colorBrushes[i],
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                };

                var colorLabel = new TextBlock {
                    Text = $"Color {i}",
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                Canvas.SetLeft(colorRect, startX);
                Canvas.SetTop(colorRect, startY + 25 + (i * 25));

                Canvas.SetLeft(colorLabel, startX + 25);
                Canvas.SetTop(colorLabel, startY + 25 + (i * 25));

                relationCanvas.Children.Add(colorRect);
                relationCanvas.Children.Add(colorLabel);
            }

            // Add chromatic number information
            var chromaticInfo = new TextBlock {
                Text = $"Chromatic Number: {colorCount}",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(chromaticInfo, startX);
            Canvas.SetTop(chromaticInfo, startY + 30 + (colorCount * 25));
            relationCanvas.Children.Add(chromaticInfo);
        }

        // Method to get client-chef relationships
        public RelationshipMap<int> GetClientChefRelationships() {
            relationshipMap = new RelationshipMap<int>();
            nodeCache = new Dictionary<int, Node<int>>();

            try {
                var query = @"SELECT DISTINCT ClientID, ChefID FROM Orders;";
                var command = new MySqlCommand(query);
                using (var reader = _mySQLManager.ExecuteReader(command)) {
                    while (reader.Read()) {
                        int clientId = reader.GetInt32("ClientID");
                        int chefId = reader.GetInt32("ChefID");

                        // Get or create nodes from cache to ensure we use the same node instance
                        if (!nodeCache.TryGetValue(clientId, out Node<int> clientNode)) {
                            clientNode = new Node<int>(clientId);
                            nodeCache[clientId] = clientNode;
                        }

                        if (!nodeCache.TryGetValue(chefId, out Node<int> chefNode)) {
                            chefNode = new Node<int>(chefId);
                            nodeCache[chefId] = chefNode;
                        }

                        // Add edge using the cached node instances
                        relationshipMap.AddBidirectionalEdge(clientNode, chefNode);
                    }
                }
            }
            catch (Exception ex) {
                // Handle exceptions (e.g., log error, show message box)
                MessageBox.Show($"Error loading relationship data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return relationshipMap;
        }

        #endregion

        #region my account logic

        public void BtnEditAccount_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Clicked on Edit Account button");
            var editWindow = new EditUserWindow(_mySQLManager, _currentUser, _allMetroName);
            editWindow.Owner = this; // Set the owner to keep window management clean

            if (editWindow.ShowDialog() == true) {
                // Update UI elements (e.g., tab visibility) based on updated user roles
                UpdateUIForLoggedInUser();

                // User was updated
                MessageBox.Show($"User {_currentUser.FullName} was updated successfully",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
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
                string query = "SELECT * FROM Dishes d WHERE ChefID = @ChefID";
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
            }
        }

        // Method to load orders placed by the current user
        private void LoadMyOrders() {
            LoadPlacedOrders();
            LoadReceivedOrders();
        }

        private void LoadPlacedOrders() {
            /// <summary>
            /// This load orders method is used to load the client view mode of all of his orders
            /// It should only be called ONCE at the start of the program
            /// </summary>
            Logger.Log("Loading all current user's order...");
            try {
                _placedOrders.Clear();
                var query = cmbOrderView.SelectedIndex == 0
                    ? @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
                    uc.FirstName AS ClientFirst, uc.LastName AS ClientLast, 
                    uch.FirstName AS ChefFirst, uch.LastName AS ChefLast
                FROM Orders o
                JOIN Users uc ON o.ClientID = uc.UserID
                JOIN Users uch ON o.ChefID = uch.UserID
                WHERE o.ClientID = @UserID"
                    : @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
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
                        _placedOrders.Add(new Order {
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
                Logger.Log($"Loaded {_placedOrders.Count} orders from current user");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading orders: {ex.Message}");
            }
        }

        // Method to load orders received by the current user (for chefs)
        private void LoadReceivedOrders() {
            /// <summary>
            /// This load orders method is used to load the chef view mode of all orders received
            /// It should only be called when switching to the "Orders I Received" view
            /// </summary>
            Logger.Log("Loading all orders received by current user...");
            try {
                _recievedOrders.Clear();
                var query = @"SELECT o.OrderID, o.ClientID, o.ChefID, o.Address, o.OrderDate, o.OrderTotal, o.Status,
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
                        _recievedOrders.Add(new Order {
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
                Logger.Log($"Loaded {_recievedOrders.Count} orders received by current user");
            }
            catch (Exception ex) {
                Logger.Log($"Error loading received orders: {ex.Message}");
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
                            TotalMoneySpent = reader.GetDecimal("TotalMoneySpent"),
                            TotalOrderCompleted = reader.GetDouble("TotalOrderCompleted"),
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
                            Status = reader.GetString("Status"),
                            ChefName = reader.GetString("ChefName")
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

                    //update relationMap and Json export
                    if (!nodeCache.TryGetValue(_currentUser.UserID, out Node<int> clientNode)) {
                        clientNode = new Node<int>(_currentUser.UserID);
                        nodeCache[_currentUser.UserID] = clientNode;
                    }

                    if (!nodeCache.TryGetValue(chefId, out Node<int> chefNode)) {
                        chefNode = new Node<int>(chefId);
                        nodeCache[chefId] = chefNode;
                    }
                    relationshipMap.AddBidirectionalEdge(clientNode, chefNode);
                    var algorithm = new WelshPowell<int>(relationshipMap);
                    var exporter = new GraphColorationExporter<int>(algorithm);
                    string json = exporter.ExportToJson();

                    string outputPath = System.IO.Path.Combine(GetSolutionDirectoryInfo().FullName, "LivingParisApp", "exported-data", "graph-export.json");
                    File.WriteAllText(outputPath, json);

                    DrawRelationshipMap();

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
                    _placedOrders.Add(newOrder);
                    _filteredOrders.Add(newOrder);
                    createdOrderIds.Add(orderId);

                    Logger.Log($"Created order with OrderID: {orderId} for ChefID: {chefId}");

                    // Insert each cart item into the OrderDishes table
                    foreach (var cartItem in chefItems) {
                        string insertOrderDishQuery = @"
                            INSERT INTO OrderDishes (OrderID, DishID, Quantity, OrderPrice)
                            VALUES (@OrderID, @DishID, @Quantity, @OrderPrice)";
                        var orderDishCommand = new MySqlCommand(insertOrderDishQuery);
                        orderDishCommand.Parameters.AddWithValue("@OrderID", orderId);
                        orderDishCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);
                        orderDishCommand.Parameters.AddWithValue("@Quantity", cartItem.Quantity);
                        orderDishCommand.Parameters.AddWithValue("@OrderPrice", cartItem.Dish.DishPrice);

                        _mySQLManager.ExecuteNonQuery(orderDishCommand);
                        Logger.Log($"Added dish {cartItem.Dish.DishID} to OrderDishes with quantity {cartItem.Quantity}");

                        processedItems.Add(cartItem);
                    }

                    // update dish's status from the market place
                    foreach (var cartItem in processedItems) {
                        string updateDishQuery = "UPDATE Dishes SET Status = 'Sold Out' WHERE DishID = @DishID"; // set status to sold out
                        var updateCommand = new MySqlCommand(updateDishQuery);
                        updateCommand.Parameters.AddWithValue("@DishID", cartItem.Dish.DishID);
                        int numberOfRowsAffected = _mySQLManager.ExecuteNonQuery(updateCommand, null);
                        Logger.Log($"Marked dish {cartItem.Dish.DishID} as Sold Out, number of rows affected : {numberOfRowsAffected}");

                        //now updating the dish in all collections
                        cartItem.Dish.Status = "Sold Out";

                        _filteredAvailableDishes.Remove(cartItem.Dish); // just remove it visually from the market place

                        // update total money spent by the user
                        _currentUser.TotalMoneySpent += cartItem.Dish.DishPrice;
                    }

                    Logger.Log($"TotalMoneySpent before: {_currentUser.TotalMoneySpent}");

                    string updateTotalMOneySpentQuery = @"UPDATE Users 
                        SET TotalMoneySpent = @TotalMoneySpent 
                        WHERE UserID = @UserID;";
                    var updateTotalMOneySpentCommand = new MySqlCommand(updateTotalMOneySpentQuery);
                    updateTotalMOneySpentCommand.Parameters.AddWithValue("TotalMoneySpent", _currentUser.TotalMoneySpent);
                    updateTotalMOneySpentCommand.Parameters.AddWithValue("UserID", _currentUser.UserID);

                    _mySQLManager.ExecuteNonQuery(updateTotalMOneySpentCommand);
                    Logger.Log($"Updated total user money spent. Now: {_currentUser.TotalMoneySpent}");

                    // now update the proprety in the collection
                    var userToUpdateAllUser = _allUsers.FirstOrDefault(u => u.UserID == _currentUser.UserID);
                    if (userToUpdateAllUser != null) {
                        userToUpdateAllUser.TotalMoneySpent = _currentUser.TotalMoneySpent;
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
            }
        }

        public void BtnCancelOrder_Click(object sender, EventArgs e) {
            if (sender is Button button && button.DataContext is Order selectedOrder) {
                if (selectedOrder != null) {
                    var query = $@"UPDATE Orders SET Status = 'Cancelled' WHERE OrderID = @OrderID";
                    var command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@OrderID", selectedOrder.OrderID);
                    _mySQLManager.ExecuteNonQuery(command);

                    // change the propriety in the api
                    UpdateOrderStatusInCollections("Cancelled", selectedOrder);
                }
            }
        }

        public void BtnCompleteOrder_Click(object sender, EventArgs e) {
            if (sender is Button button && button.DataContext is Order selectedOrder) {
                if (selectedOrder != null) {
                    string query = @"UPDATE Orders SET Status = 'Completed' WHERE OrderID = @OrderID";
                    var command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@OrderID", selectedOrder.OrderID);
                    _mySQLManager.ExecuteNonQuery(command);

                    // change the propriety in the api
                    var CurrentUserToEdit = _allUsers.FirstOrDefault(u => u.UserID == _currentUser.UserID);
                    if (CurrentUserToEdit != null) {
                        CurrentUserToEdit.TotalOrderCompleted += 1;
                    }
                    UpdateOrderStatusInCollections("Completed", selectedOrder);

                    //save in the database
                    query = @$"UPDATE Users SET TotalOrderCompleted = {CurrentUserToEdit.TotalOrderCompleted} WHERE UserID = @UserID";
                    command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@UserID", CurrentUserToEdit.UserID);
                    _mySQLManager.ExecuteNonQuery(command);
                }
            }
        }

        public void BtnRefuseOrder_Click(object sender, EventArgs e) {
            if (sender is Button button && button.DataContext is Order selectedOrder) {
                if (selectedOrder != null) {
                    string query = @"
                            UPDATE Orders SET Status = 'Refused' WHERE OrderID = @OrderID";
                    var command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@OrderID", selectedOrder.OrderID);
                    _mySQLManager.ExecuteNonQuery(command);

                    // change the propriety in the api
                    UpdateOrderStatusInCollections("Refused", selectedOrder);
                }
            }
        }

        public void BtnAddToCart_Click(object sender, RoutedEventArgs e) {
            Logger.Log("Add to cart button clicked");
            try {
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
                Logger.Success("Added item in the cart");
            }
            catch (Exception ex) {
                Logger.Error(ex.ToString());
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
                    }
                }
            }
        }

        public void BtnViewOrderDetails_Click(object sender = null, RoutedEventArgs e = null) {
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
                        Logger.Warning("Cannot display route: Client or Chef's closest metro station is not set.");
                        return;
                    }

                    // Step 4: Find the vertices for the client's and chef's metro stations
                    Node<MetroStation>? clientNode = _map.AdjacencyList.Keys
                        .FirstOrDefault(v => v.Object?.LibelleStation == clientMetro);
                    Node<MetroStation>? chefNode = _map.AdjacencyList.Keys
                        .FirstOrDefault(v => v.Object?.LibelleStation == chefMetro);

                    if (clientNode == null || chefNode == null) {
                        Logger.Warning("Cannot display route: Client or Chef's metro station not found in the map.");
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
                    // Step 5: Use A* to find the shortest path
                    var aStar = new Astar<MetroStation>();
                    var (path, totalLength) = aStar.Run(_map, chefNode, clientNode);

                    if (path == null || path.Count == 0) {
                        Logger.Warning($"No path found between the client's ({clientNode.Object.LibelleStation}) and chef's ({chefNode.Object.LibelleStation}) metro stations.");
                        return;
                    }

                    // Step 6: Redraw the map with the path highlighted
                    DrawHighlightedEdges(path);
                }
                catch (Exception ex) {
                    Logger.Log($"Error viewing order details: {ex.Message}");
                }
            }
        }

        public void BtnViewDishDetails_Click(object sender = null, RoutedEventArgs e = null) {
            Logger.Log("View DishDetails details button clicked");

            if (sender is Button button && button.DataContext is Dish selectedDish) {
                try {
                    Logger.Log("selected dish id: ", selectedDish.DishID);
                    // Step 1: Retrieve the list of ingredients in the dish
                    var ingredients = new List<(string Name, int Quantity, string MeasurementType)>();
                    string query = @"
                        SELECT i.Name, 
                            CASE 
                                WHEN di.Grams IS NOT NULL THEN di.Grams
                                WHEN di.Pieces IS NOT NULL THEN di.Pieces
                                ELSE NULL
                            END AS Quantity,
                            CASE 
                                WHEN di.Grams IS NOT NULL THEN 'Grams'
                                WHEN di.Pieces IS NOT NULL THEN 'Pieces'
                                ELSE NULL
                            END AS MeasurementType
                        FROM Dishes d
                        JOIN DishIngredients di ON d.DishID = di.DishID
                        JOIN Ingredients i ON di.IngredientID = i.IngredientID
                        WHERE d.DishID = @DishID;";
                    var command = new MySqlCommand(query);
                    command.Parameters.AddWithValue("@DishID", selectedDish.DishID);

                    using (var reader = _mySQLManager.ExecuteReader(command)) {
                        while (reader.Read()) {
                            ingredients.Add((
                                Name: reader.GetString("Name"),
                                Quantity: reader.GetInt32("Quantity"),
                                MeasurementType: reader.GetString("MeasurementType")
                            ));
                        }
                    }
                    Logger.Log(ingredients.Count);
                    // Step 2: Display dish details
                    string details = $"Name: {selectedDish.Name:dd/MM/yyyy}\n" +
                                    "Ingredients:\n";
                    if (ingredients.Count == 0) {
                        details += "Not Specified";
                    }
                    else {
                        foreach (var ingredient in ingredients) {
                            details += $"{ingredient.Quantity} {ingredient.MeasurementType} of {ingredient.Name}\n";
                        }
                    }

                    MessageBox.Show(details, "Dish Details");
                }
                catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }
        public void TxtSearchDishName_KeyDown(object sender, KeyEventArgs e) {
            //Logger.Log($"Key pressed: {e.Key}");
            BtnClearFiltersDishes_Click(sender, e); // Call the existing click handler
        }

        private void TxtSearchDishName_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Back) {
                //Logger.Log($"Key pressed: {e.Key}");
                BtnClearFiltersDishes_Click(sender, e); // Call the existing click handler
            }
        }

        public void BtnApplyFiltersDishes_Click(object sender = null, RoutedEventArgs e = null) {
            /// <summary>
            /// This applies the filter on all available dishes IN the MARKETPLACE
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>

            if (cmbDishType == null || cmbDiet == null || cmbOrigin == null) {
                Logger.Warning("Filter controls are not accessible");
                return;
            }

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

            // then search by name
            string searchDish = txtSearchDishName.Text.Trim().ToLower();

            if (!string.IsNullOrEmpty(searchDish)) {
                // Fuzzy match using Levenshtein distance
                filteredAvailableDishes = filteredAvailableDishes
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

            // Remove duplicates that might have matched multiple criteria
            filteredAvailableDishes = filteredAvailableDishes.Where(d => d.Status == "Available");
            filteredAvailableDishes = filteredAvailableDishes.Distinct();

            foreach (Dish dish in filteredAvailableDishes) {
                _filteredAvailableDishes.Add(dish);
            }

            // Show message if no results found
            if (!_filteredAvailableDishes.Any()) {
                Logger.Warning("No dishes found with these filters");
            }
        }

        public void BtnClearFiltersDishes_Click(object sender = null, RoutedEventArgs e = null) {
            cmbDishType.SelectedIndex = 0;
            cmbDiet.SelectedIndex = 0;
            cmbOrigin.SelectedIndex = 0;

            // apply default filters
            BtnApplyFiltersDishes_Click();
        }
        #endregion

        #region Admin
        // Admin Tab - User Management
        private void TxtSearchUser_KeyDown(object sender, KeyEventArgs e) {
            BtnSearchUser_Click(sender, e);
        }

        private void TxtSearchUser_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Back) {
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
                Logger.Warning("No users found matching the search criteria.");
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag != null) {
                string userId = button.Tag.ToString();
                // Implement edit user logic
                // Possibly open a dialog with user details for editing
                User userToEdit = _allUsers.FirstOrDefault(u => u.UserID.ToString() == userId);

                if (userToEdit != null) {
                    var editWindow = new EditUserWindow(_mySQLManager, userToEdit, _allMetroName);
                    editWindow.Owner = this; // Set the owner to keep window management clean

                    if (editWindow.ShowDialog() == true) {
                        if (userToEdit.UserID == _currentUser.UserID) {
                            _currentUser = userToEdit;
                            UpdateUIForLoggedInUser();
                        }
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
                    }
                }
            }
        }

        // Admin Tab - Dish Management
        private void TxtSearchDish_KeyDown(object sender, KeyEventArgs e) {
            BtnSearchDish_Click(sender, e);
        }

        private void TxtSearchDish_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Back) {
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

            if (!filteredDishes.Any()) {
                Logger.Warning("No dishes found matching the search criteria.");
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

            // Apply date range filter if dates are selected
            if (dpFromDate.SelectedDate.HasValue) {
                DateTime fromDate = dpFromDate.SelectedDate.Value.Date;
                filteredOrders = filteredOrders.Where(o => o.OrderDate.Date >= fromDate);
            }

            if (dpToDate.SelectedDate.HasValue) {
                DateTime toDate = dpToDate.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
                filteredOrders = filteredOrders.Where(o => o.OrderDate.Date <= toDate);
            }

            // Update ObservableCollection
            foreach (var order in filteredOrders) {
                _filteredOrders.Add(order);
            }

            // Show message if no results found
            if (!filteredOrders.Any()) {
                Logger.Warning("No orders found matching the search criteria.");
            }
            else {
                Logger.Log($"Loaded {_filteredOrders.Count} orders after filter");
            }
        }

        private void BtnClearFiltersOrders_Click(object sender, RoutedEventArgs e) {
            // Reset filters
            cmbOrderStatus.SelectedIndex = 0;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;

            // Reload all orders with the default filter
            BtnFilterOrders_Click();
        }

        private void BtnViewAdminOrder_Click(object sender, RoutedEventArgs e) {
            // Implement view order details logic
            // Possibly open a dialog showing order details
            BtnViewOrderDetails_Click(sender, e);
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

        #region SelectionChange

        private void CmbOrderView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            try {
                if (cmbOrderView == null || dgOrdersPlaced == null || dgOrdersReceived == null) return;

                if (cmbOrderView.SelectedIndex == 0) { // Orders I Placed 
                    dgOrdersPlaced.Visibility = Visibility.Visible;
                    dgOrdersReceived.Visibility = Visibility.Collapsed;

                    // Load "Orders I Placed" data
                    LoadPlacedOrders();

                    Logger.Log("Viewing placed orders");
                }
                else { // Orders I Received            
                    dgOrdersPlaced.Visibility = Visibility.Collapsed;
                    dgOrdersReceived.Visibility = Visibility.Visible;

                    // Load "Orders I Received" data
                    LoadReceivedOrders();

                    Logger.Log("Viewing recieved orders");
                }
            }
            catch (Exception ex) {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Collection Macros 

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

        private void UpdateOrderStatusInCollections(string newStatus, Order order) {
            // Remove the dish from _allDishes
            var a = _allOrders.FirstOrDefault(o => o.OrderID == order.OrderID);
            if (a != null) {
                a.Status = newStatus;
            }

            // Remove the dish from _myDishes
            var b = _placedOrders.FirstOrDefault(o => o.OrderID == order.OrderID);
            if (b != null) {
                b.Status = newStatus;
            }

            // Remove the dish from _filteredDishes // admin view
            var c = _recievedOrders.FirstOrDefault(o => o.OrderID == order.OrderID);
            if (c != null) {
                c.Status = newStatus;
            }

            // Remove the dish from _filteredDishes // admin view
            var d = _filteredOrders.FirstOrDefault(o => o.OrderID == order.OrderID);
            if (d != null) {
                d.Status = newStatus;
            }
        }

        private void UpdateCartTotal() {
            decimal total = _cartItems.Sum(i => i.TotalPrice);
            txtCartTotal.Text = total.ToString("C2");
        }

        #endregion
    }
}