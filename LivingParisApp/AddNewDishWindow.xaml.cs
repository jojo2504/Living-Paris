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

            //load ingredients
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            try {
                // Validate inputs
                if (string.IsNullOrEmpty(txtName.Text) ||
                    string.IsNullOrEmpty(cmbType.Text) ||
                    string.IsNullOrEmpty(txtPrice.Text)) {
                    MessageBox.Show("Please fill in all required fields (Name, Type, Price)",
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

                // Create new dish object
                var newDish = new Dish {
                    Name = txtName.Text,
                    Type = cmbType.Text,
                    DishPrice = price,
                    FabricationDate = DateTime.Now,
                    PeremptionDate = DateTime.Now.AddMonths(1), // Default expiration 1 month from now
                    Diet = txtDiet.Text,
                    Origin = txtOrigin.Text
                };

                // Insert into database
                string query = @"INSERT INTO Dishes (ChefID, Name, Type, DishPrice, FabricationDate, 
                              PeremptionDate, Diet, Origin) 
                              VALUES (@ChefID, @Name, @Type, @DishPrice, @FabricationDate, 
                              @PeremptionDate, @Diet, @Origin)";

                var command = new MySqlCommand(query);
                command.Parameters.AddWithValue("@ChefID", _currentUser.UserID);
                command.Parameters.AddWithValue("@Name", newDish.Name);
                command.Parameters.AddWithValue("@Type", newDish.Type);
                command.Parameters.AddWithValue("@DishPrice", newDish.DishPrice);
                command.Parameters.AddWithValue("@FabricationDate", newDish.FabricationDate);
                command.Parameters.AddWithValue("@PeremptionDate", newDish.PeremptionDate);
                command.Parameters.AddWithValue("@Diet", newDish.Diet);
                command.Parameters.AddWithValue("@Origin", newDish.Origin);

                _mySQLManager.ExecuteNonQuery(command);

                Logger.Log($"New dish added: {newDish.Name}");
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