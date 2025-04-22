using System.Collections.ObjectModel;
using System.Windows;
using LivingParisApp.Core.Models.Food;
using LivingParisApp.Core.Models.Human;
using LivingParisApp.Services.Logging;
using LivingParisApp.Services.MySQL;
using MySql.Data.MySqlClient;

namespace LivingParisApp {
    public partial class AddNewDishWindow : Window {
        private readonly MySQLManager _mySQLManager;
        private readonly User _currentUser;
        private readonly Dish _dishToEdit;  // null if adding new dish
        private ObservableCollection<DishIngredient> _ingredients;

        // Property to store the created dish
        public Dish CreatedDish { get; private set; }

        public AddNewDishWindow(MySQLManager mySQLManager, User currentUser, Dish dishToEdit = null) {
            InitializeComponent();
            _mySQLManager = mySQLManager;
            _currentUser = currentUser;
            _dishToEdit = dishToEdit;
            _ingredients = new ObservableCollection<DishIngredient>();
            //lstIngredients.ItemsSource = _ingredients;
            //LoadAvailableIngredients();
            if (_dishToEdit != null) {
                LoadDishForEditing();
            }
        }

        private void LoadDishForEditing() {
            txtName.Text = _dishToEdit.Name;
            cmbType.Text = _dishToEdit.Type;
            txtPrice.Text = _dishToEdit.DishPrice.ToString();
            txtDiet.Text = _dishToEdit.Diet;
            txtOrigin.Text = _dishToEdit.Origin;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            try {
                // Validate inputs
                if (string.IsNullOrEmpty(txtName.Text) ||
                    string.IsNullOrEmpty(cmbType.Text) ||
                    string.IsNullOrEmpty(txtPrice.Text) ||
                    string.IsNullOrEmpty(txtDiet.Text) ||
                    string.IsNullOrEmpty(txtOrigin.Text)) {
                    MessageBox.Show("Please fill in all required fields (Name, Type, Price, Diet, Origin)",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPrice.Text, out decimal price)) {
                    MessageBox.Show("Please enter a valid price",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                string query;
                if (_dishToEdit is null) {
                    // Insert into database
                    query = @"INSERT INTO Dishes (ChefID, Name, Type, DishPrice, FabricationDate, PeremptionDate, Diet, Origin) 
                            VALUES (@ChefID, @Name, @Type, @DishPrice, @FabricationDate, @PeremptionDate, @Diet, @Origin);
                            SELECT LAST_INSERT_ID();";
                }
                else {
                    // Update existing dish
                    query = @"UPDATE Dishes 
                        SET ChefID = @ChefID, 
                            Name = @Name, 
                            Type = @Type, 
                            DishPrice = @DishPrice, 
                            FabricationDate = @FabricationDate, 
                            PeremptionDate = @PeremptionDate, 
                            Diet = @Diet, 
                            Origin = @Origin 
                        WHERE DishID = @DishID";
                }

                var command = new MySqlCommand(query);
                if (_dishToEdit is not null) { // editing
                    command.Parameters.AddWithValue("@DishID", _dishToEdit.DishID);
                }
                command.Parameters.AddWithValue("@ChefID", _currentUser.UserID);
                command.Parameters.AddWithValue("@Name", txtName.Text);
                command.Parameters.AddWithValue("@Type", cmbType.Text);
                command.Parameters.AddWithValue("@DishPrice", price);
                command.Parameters.AddWithValue("@FabricationDate", DateTime.Now);
                command.Parameters.AddWithValue("@PeremptionDate", DateTime.Now.AddMonths(1));
                command.Parameters.AddWithValue("@Diet", txtDiet.Text);
                command.Parameters.AddWithValue("@Origin", txtOrigin.Text);


                // now create / update the objects to the collections
                if (_dishToEdit is not null){ // editing
                    _mySQLManager.ExecuteNonQuery(command);

                    _dishToEdit.Name = txtName.Text;
                    _dishToEdit.Type = cmbType.Text;
                    _dishToEdit.DishPrice = price;
                    _dishToEdit.FabricationDate = DateTime.Now;
                    _dishToEdit.PeremptionDate = DateTime.Now.AddMonths(1);
                    _dishToEdit.Diet = txtDiet.Text;    
                    _dishToEdit.Origin = txtOrigin.Text;

                    Logger.Log("Edited the dish");
                }
                else {
                    var dishId = _mySQLManager.ExecuteScalar(command);
                    // Create new dish object
                    var newDish = new Dish {
                        DishID = Convert.ToInt32(dishId),
                        ChefID = _currentUser.UserID,
                        Name = txtName.Text,
                        Type = cmbType.Text,
                        DishPrice = price,
                        FabricationDate = DateTime.Now,
                        PeremptionDate = DateTime.Now.AddMonths(1), // Default expiration 1 month from now
                        Diet = txtDiet.Text,
                        Origin = txtOrigin.Text,
                        ChefName = _currentUser.FullName
                    };
                    Logger.Log($"New dish added: {newDish.Name}");
                    CreatedDish = newDish;
                }

                DialogResult = true; // Indicate success
                Close();
            }
            catch (Exception ex) {
                Logger.Log($"Error adding new dish: {ex.Message}");
                MessageBox.Show($"Error saving dish: {ex.Message}",
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