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

namespace LivingParisApp {
    public partial class MainWindow : Window {
        MySQLManager _mySQLManager;
        private User _currentUser;
        Map<MetroStation> _map;
        private double minLat, maxLat, minLon, maxLon;
        private readonly Dictionary<Node<MetroStation>, Point> stationCoordinates = new Dictionary<Node<MetroStation>, Point>();

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

            InitializeMapTransforms();
            LoadInitialData();

            cmbDishType.ItemsSource = DishTypes;
            cmbDiet.ItemsSource = Diets;
            cmbOrigin.ItemsSource = Origins;

            this.Loaded += (sender, e) => RenderMap();
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

            // Clear sign in fields
            txtSignInEmail.Text = string.Empty;
            pwdSignIn.Password = string.Empty;
            txtSignInStatus.Text = string.Empty;

            if (tabAccount.Parent is TabControl tabControl) {
                // Select the sign in tab
                tabControl.SelectedIndex = 1;
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
                           p.Postcode, p.City, p.PhoneNumber, p.ClosestMetro, p.TotalMoneySpent, p.IsClient, p.IsChef
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
                            ClosestMetro = userReader["ClosestMetro"] == DBNull.Value ? "" : (string)userReader["ClosestMetro"],
                            IsChef = (int)userReader["IsChef"],
                            IsClient = (int)userReader["IsClient"]
                        };
                        UpdateUIForLoggedInUser();
                    }
                }

                // Switch to the Sign In tab (assuming TabControl is the main control)
                // Get the parent TabControl
                if (tabAccount.Parent is TabControl tabControl) {
                    // Select the first tab (Sign In tab)
                    tabControl.SelectedIndex = 4;
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
            if (string.IsNullOrWhiteSpace(txtClosestMetro.Text)) {
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

        private void RenderMap(LinkedList<Node<MetroStation>> path = null) {
            try {
                metroCanvas.Children.Clear();

                if (_map?.AdjacencyList == null || _map.AdjacencyList.Count == 0) {
                    return;
                }

                InitMapBounds();

                // Extract all valid stations with coordinates
                var stations = _map.AdjacencyList.Keys
                    .Where(node => node?.Object != null &&
                          node.Object.Longitude != 0 &&
                          node.Object.Latitude != 0)
                    .ToList();

                if (!stations.Any()) {
                    return;
                }

                // draw all the nodes
                foreach (var station in stations) {
                    DrawNode(station);
                }

                // draw all the edges
                DrawEdges(path, stations);
            }
            catch (Exception ex) {
                Logger.Error(ex.Message);
            }
        }

        private void InitMapBounds() {
            var nodeList = _map.AdjacencyList.Keys.ToList();
            minLat = maxLat = nodeList[0].Object.Latitude;
            minLon = maxLon = nodeList[0].Object.Longitude;

            foreach (var node in _map.AdjacencyList.Keys) {
                minLat = Math.Min(minLat, node.Object.Latitude);
                maxLat = Math.Max(maxLat, node.Object.Latitude);
                minLon = Math.Min(minLon, node.Object.Longitude);
                maxLon = Math.Max(maxLon, node.Object.Longitude);
            }

            // Add a small buffer
            double latBuffer = (maxLat - minLat) * 0.05;
            double lonBuffer = (maxLon - minLon) * 0.05;

            minLat -= latBuffer;
            maxLat += latBuffer;
            minLon -= lonBuffer;
            maxLon += lonBuffer;
        }


        private void DrawNode(Node<MetroStation> node) {
            try {
                double x = ScaleLongitude(node.Object.Longitude);
                double y = ScaleLatitude(node.Object.Latitude);

                // Store position for links
                stationCoordinates[node] = new Point(x, y);

                // Create station visual with drop shadow
                var shadowEffect = new DropShadowEffect {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.6
                };

                // Station marker with gradient
                var gradientStops = new GradientStopCollection {
                    new GradientStop(Colors.White, 0.0),
                    new GradientStop(GetStationColor(node.Object.LibelleLine), 1.0)
                };

                Ellipse nodeEllipse = new Ellipse {
                    Width = 18,
                    Height = 18,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Colors.White),
                    Fill = new RadialGradientBrush(gradientStops),
                    Effect = shadowEffect
                };

                // Add white border for better contrast
                Ellipse outerRing = new Ellipse {
                    Width = 22,
                    Height = 22,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Colors.Black),
                    Fill = new SolidColorBrush(Colors.Transparent)
                };

                // Position the node elements
                Canvas.SetLeft(outerRing, x - outerRing.Width / 2);
                Canvas.SetTop(outerRing, y - outerRing.Height / 2);
                Canvas.SetLeft(nodeEllipse, x - nodeEllipse.Width / 2);
                Canvas.SetTop(nodeEllipse, y - nodeEllipse.Height / 2);
                Panel.SetZIndex(outerRing, 5);
                Panel.SetZIndex(nodeEllipse, 10);

                metroCanvas.Children.Add(outerRing);
                metroCanvas.Children.Add(nodeEllipse);

                // Create the node label with better visibility
                TextBlock nodeLabel = new TextBlock {
                    Text = node.Object.LibelleStation,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.Black),
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    Padding = new Thickness(3, 1, 3, 1)
                };

                // Create a border for the text to improve readability
                Border labelBorder = new Border {
                    Child = nodeLabel,
                    CornerRadius = new CornerRadius(3),
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1)
                };

                // Position the label
                Canvas.SetLeft(labelBorder, x + 12);
                Canvas.SetTop(labelBorder, y - 12);
                Panel.SetZIndex(labelBorder, 15);

                // Add to canvas
                metroCanvas.Children.Add(labelBorder);

                // Add mouse hover behavior for better UX
                AddHoverBehavior(nodeEllipse, labelBorder, node);
            }
            catch (Exception ex) {
                Logger.Fatal(ex);
            }
        }

        private void AddHoverBehavior(Ellipse nodeEllipse, Border labelBorder, Node<MetroStation> node) {
            // Create animation for hover effect
            nodeEllipse.MouseEnter += (s, e) => {
                nodeEllipse.Width = 24;
                nodeEllipse.Height = 24;
                Canvas.SetLeft(nodeEllipse, Canvas.GetLeft(nodeEllipse) - 3);
                Canvas.SetTop(nodeEllipse, Canvas.GetTop(nodeEllipse) - 3);

                // Highlight label
                labelBorder.BorderBrush = new SolidColorBrush(Colors.DarkBlue);
                labelBorder.Background = new SolidColorBrush(Colors.LightYellow);

                // Show station info tooltip
                ToolTip tooltip = new ToolTip {
                    Content = $"Station: {node.Object.LibelleStation}\nLigne: {node.Object.LibelleLine}"
                };
                nodeEllipse.ToolTip = tooltip;
            };

            nodeEllipse.MouseLeave += (s, e) => {
                nodeEllipse.Width = 18;
                nodeEllipse.Height = 18;
                Canvas.SetLeft(nodeEllipse, Canvas.GetLeft(nodeEllipse) + 3);
                Canvas.SetTop(nodeEllipse, Canvas.GetTop(nodeEllipse) + 3);

                // Restore label
                labelBorder.BorderBrush = new SolidColorBrush(Colors.Gray);
                labelBorder.Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            };
        }

        // Helper method to get color based on metro line
        private Color GetStationColor(string lineCode) {
            if (string.IsNullOrEmpty(lineCode)) return Colors.RosyBrown;

            // Create a color mapping for different metro lines
            var colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase) {
                { "1", Colors.DarkBlue },
                { "2", Colors.Red },
                { "3", Colors.Green },
                { "4", Colors.Purple },
                { "5", Colors.Orange },
                { "6", Colors.Teal },
                { "7", Colors.Pink },
                { "8", Colors.YellowGreen },
                { "9", Colors.Brown },
                { "10", Colors.DeepSkyBlue },
                // Add more lines as needed
            };

            return colorMap.TryGetValue(lineCode, out var color) ? color : Colors.RosyBrown;
        }

        private double ScaleLongitude(double longitude) {
            double width = metroCanvas.Width;
            // Basic linear mapping is fine for longitude
            return (longitude - minLon) / (maxLon - minLon) * width;
        }

        private double ScaleLatitude(double latitude) {
            double height = metroCanvas.Height;

            // Convert latitude to Mercator projection
            double latRad = latitude * Math.PI / 180; // Convert to radians
            double mercatorY = Math.Log(Math.Tan(Math.PI / 4 + latRad / 2));

            // Calculate the Mercator Y values for min and max latitudes
            double minLatRad = minLat * Math.PI / 180;
            double maxLatRad = maxLat * Math.PI / 180;
            double minMercatorY = Math.Log(Math.Tan(Math.PI / 4 + minLatRad / 2));
            double maxMercatorY = Math.Log(Math.Tan(Math.PI / 4 + maxLatRad / 2));

            // Scale and invert
            return height - ((mercatorY - minMercatorY) / (maxMercatorY - minMercatorY)) * height;
        }

        private void DrawEdges(LinkedList<Node<MetroStation>> path, List<Node<MetroStation>> stations) {
            // Create path edge list for highlighting
            var pathEdges = new List<(Node<MetroStation> Start, Node<MetroStation> End)>();
            if (path != null && path.Count > 1) {
                var current = path.First;
                while (current?.Next != null) {
                    pathEdges.Add((current.Value, current.Next.Value));
                    current = current.Next;
                }
            }

            // Group connections by line for better visualization
            var lineConnections = new Dictionary<string, List<(Point Start, Point End, bool IsPath)>>();

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

                    // Get line code and normalize it
                    string lineCode = stationNode.Object.LibelleLine ?? "default";

                    // Add to line connections
                    if (!lineConnections.ContainsKey(lineCode))
                        lineConnections[lineCode] = new List<(Point, Point, bool)>();

                    lineConnections[lineCode].Add((startPoint, endPoint, isPathConnection));
                }
            }

            // Draw connections by line
            foreach (var linePair in lineConnections) {
                string lineCode = linePair.Key;
                var connections = linePair.Value;

                Color baseColor = GetStationColor(lineCode);
                Brush regularBrush = new SolidColorBrush(baseColor);

                // Create highlighted path brush with glow effect
                LinearGradientBrush highlightBrush = new LinearGradientBrush {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                highlightBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
                highlightBrush.GradientStops.Add(new GradientStop(Colors.Orange, 1.0));

                foreach (var (start, end, isPath) in connections) {
                    // Create beautiful curved line with appropriate styling
                    Path linePath = new Path();

                    // Define geometry for bezier curve
                    PathGeometry geometry = new PathGeometry();
                    PathFigure figure = new PathFigure { StartPoint = start };

                    // Calculate control points for slight curve
                    Vector direction = end - start;
                    double distance = direction.Length;
                    Vector perpendicular = new Vector(-direction.Y, direction.X);
                    perpendicular.Normalize();
                    perpendicular *= distance * 0.05; // Curve amount

                    Point controlPoint1 = start + (direction * 0.33) + perpendicular;
                    Point controlPoint2 = start + (direction * 0.66) - perpendicular;

                    figure.Segments.Add(new BezierSegment(controlPoint1, controlPoint2, end, true));
                    geometry.Figures.Add(figure);

                    linePath.Data = geometry;
                    linePath.Stroke = isPath ? highlightBrush : regularBrush;
                    linePath.StrokeThickness = isPath ? 6 : 4;
                    linePath.StrokeStartLineCap = PenLineCap.Round;
                    linePath.StrokeEndLineCap = PenLineCap.Round;

                    if (isPath) {
                        // Add glow effect for highlighted path
                        linePath.Effect = new DropShadowEffect {
                            Color = Colors.Gold,
                            Direction = 0,
                            ShadowDepth = 0,
                            BlurRadius = 10,
                            Opacity = 0.7
                        };
                    }

                    Panel.SetZIndex(linePath, isPath ? 3 : 1);
                    metroCanvas.Children.Add(linePath);
                }
            }

            // Add legend for metro lines (optional)
            CreateLineLegend(lineConnections.Keys.ToList());
        }

        private void CreateLineLegend(List<string> lineNames) {
            // Remove any existing legend
            var existingLegend = metroCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Name == "LineLegend");
            if (existingLegend != null)
                metroCanvas.Children.Remove(existingLegend);

            // Create legend panel
            StackPanel legendPanel = new StackPanel {
                Orientation = Orientation.Vertical,
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255))
            };

            // Add legend title
            TextBlock legendTitle = new TextBlock {
                Text = "Metro Line",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            legendPanel.Children.Add(legendTitle);

            // Add separator
            legendPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 5) });

            // Add line entries
            foreach (var line in lineNames.OrderBy(l => l)) {
                StackPanel lineEntry = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                Rectangle colorBox = new Rectangle {
                    Width = 15,
                    Height = 15,
                    Fill = new SolidColorBrush(GetStationColor(line)),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock lineLabel = new TextBlock {
                    Text = $"Line {line}",
                    VerticalAlignment = VerticalAlignment.Center
                };

                lineEntry.Children.Add(colorBox);
                lineEntry.Children.Add(lineLabel);
                legendPanel.Children.Add(lineEntry);
            }

            // Create border for legend
            Border legendBorder = new Border {
                Name = "LineLegend",
                Child = legendPanel,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Effect = new DropShadowEffect {
                    Color = Colors.Gray,
                    Direction = 315,
                    ShadowDepth = 3,
                    BlurRadius = 5,
                    Opacity = 0.5
                }
            };

            // Position legend in top-right corner
            Canvas.SetRight(legendBorder, -50);
            Canvas.SetTop(legendBorder, 10);
            Panel.SetZIndex(legendBorder, 100);

            metroCanvas.Children.Add(legendBorder);
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
                    /*// Step 5: Use A* to find the shortest path
                    var aStar = new Astar<MetroStation>();
                    var (path, totalLength) = aStar.Run(_map, chefNode, clientNode);
                    */
                    var dijkstra = new Dijkstra<MetroStation>();
                    dijkstra.Init(_map, chefNode);
                    var (path, totalLength) = dijkstra.GetPath(clientNode);

                    if (path == null || path.Count == 0) {
                        Logger.Warning($"No path found between the client's ({clientNode.Object.LibelleStation}) and chef's ({chefNode.Object.LibelleStation}) metro stations.");
                        return;
                    }

                    // Step 6: Redraw the map with the path highlighted
                    RenderMap(path);

                    // Step 7: Switch to the Map tab to show the path
                    if (metroMap.Parent is TabControl tabControl) {
                        tabControl.SelectedItem = metroMap;
                    }
                }
                catch (Exception ex) {
                    Logger.Log($"Error viewing order details: {ex.Message}");
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
                Logger.Warning("Filter cont rols are not accessible");
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