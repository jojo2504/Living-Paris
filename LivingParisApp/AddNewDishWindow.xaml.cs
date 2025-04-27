using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LivingParisApp.Core.Entities.Models;
using LivingParisApp.Services.Logging;
using LivingParisApp.Services.MySQL;
using MySql.Data.MySqlClient;

namespace LivingParisApp {
    public partial class AddNewDishWindow : Window {
        private readonly MySQLManager _mySQLManager;
        private readonly User _currentUser;
        private readonly Dish _dishToEdit;  // null if adding new dish
        private ObservableCollection<DishIngredient> _ingredients;
        private int ingredientCount = 1;

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

                try {
                    string query;
                    int dishId;

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
                        dishId = _dishToEdit.DishID;
                    }
                    command.Parameters.AddWithValue("@ChefID", _currentUser.UserID);
                    command.Parameters.AddWithValue("@Name", txtName.Text);
                    command.Parameters.AddWithValue("@Type", cmbType.Text);
                    command.Parameters.AddWithValue("@DishPrice", price);
                    command.Parameters.AddWithValue("@FabricationDate", DateTime.Now);
                    command.Parameters.AddWithValue("@PeremptionDate", DateTime.Now.AddMonths(1));
                    command.Parameters.AddWithValue("@Diet", txtDiet.Text);
                    command.Parameters.AddWithValue("@Origin", txtOrigin.Text);

                    // Execute dish creation/update
                    if (_dishToEdit is not null) { // editing
                        _mySQLManager.ExecuteNonQuery(command);
                        dishId = _dishToEdit.DishID;

                        // Remove existing ingredients for this dish before adding new ones
                        var deleteIngredientsCommand = new MySqlCommand(
                            "DELETE FROM DishIngredients WHERE DishID = @DishID");
                        deleteIngredientsCommand.Parameters.AddWithValue("@DishID", dishId);
                        _mySQLManager.ExecuteNonQuery(deleteIngredientsCommand);

                        _dishToEdit.Name = txtName.Text;
                        _dishToEdit.Type = cmbType.Text;
                        _dishToEdit.DishPrice = price;
                        _dishToEdit.FabricationDate = DateTime.Now;
                        _dishToEdit.PeremptionDate = DateTime.Now.AddMonths(1);
                        _dishToEdit.Diet = txtDiet.Text;
                        _dishToEdit.Origin = txtOrigin.Text;
                    }
                    else {
                        var result = _mySQLManager.ExecuteScalar(command);
                        dishId = Convert.ToInt32(result);
                    }

                    // Process ingredients
                    foreach (var ingredientRow in ingredientsPanel.Children.OfType<Grid>().ToList()) {

                        Logger.Log("ingredientRow", ingredientRow.ToString());

                        // Skip the button at the end
                        if (ingredientRow.Children.Count < 2)
                            continue;

                        var nameTextBox = ingredientRow.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name?.StartsWith("txtIngredientName") == true);
                        var amountTextBox = ingredientRow.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name?.StartsWith("txtIngredientAmount") == true);
                        var measureTypeComboBox = ingredientRow.Children.OfType<ComboBox>().FirstOrDefault(cb => cb.Name?.StartsWith("cmbMeasureType") == true);
                        
                        if (nameTextBox == null || amountTextBox == null || measureTypeComboBox == null)
                            continue;

                        // Skip empty ingredients
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(amountTextBox.Text))
                            continue;

                        // Get or create ingredient
                        var ingredientId = EnsureIngredientExists(nameTextBox.Text);

                        // Parse amount
                        if (!int.TryParse(amountTextBox.Text, out int amount))
                            continue;

                        // Add to DishIngredients
                        var measureType = (measureTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                        string ingredientQuery = @"
                            INSERT INTO DishIngredients (IngredientID, DishID, Grams, Pieces) 
                            VALUES (@IngredientID, @DishID, @Grams, @Pieces)";;
                        var addIngredientCommand = new MySqlCommand(ingredientQuery);
                        // Set either Gramme or Pieces based on the selected measure type
                        if (measureType == "Grams") {
                            addIngredientCommand.Parameters.AddWithValue("@Grams", amount);
                            addIngredientCommand.Parameters.AddWithValue("@Pieces", DBNull.Value);
                        }
                        else {
                            addIngredientCommand.Parameters.AddWithValue("@Grams", DBNull.Value);
                            addIngredientCommand.Parameters.AddWithValue("@Pieces", amount);
                        }
                        addIngredientCommand.Parameters.AddWithValue("@IngredientID", ingredientId);
                        addIngredientCommand.Parameters.AddWithValue("@DishID", dishId);

                        _mySQLManager.ExecuteNonQuery(addIngredientCommand);
                    }

                    // Create dish object
                    if (_dishToEdit is null) {
                        var newDish = new Dish {
                            DishID = dishId,
                            ChefID = _currentUser.UserID,
                            Name = txtName.Text,
                            Type = cmbType.Text,
                            DishPrice = price,
                            FabricationDate = DateTime.Now,
                            PeremptionDate = DateTime.Now.AddMonths(1),
                            Diet = txtDiet.Text,
                            Origin = txtOrigin.Text,
                            ChefName = _currentUser.FullName
                        };
                        Logger.Log($"New dish added: {newDish.Name}");
                        CreatedDish = newDish;
                    }
                    else {
                        Logger.Log("Edited the dish");
                    }

                    DialogResult = true; // Indicate success
                    Close();
                }
                catch (Exception ex) {
                    Logger.Error(ex); // Re-throw to be caught by outer try/catch
                }
            }
            catch (Exception ex) {
                Logger.Log($"Error adding new dish: {ex.Message}");
                MessageBox.Show($"Error saving dish: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // Helper method to get or create an ingredient
        private int EnsureIngredientExists(string ingredientName) {
            // First check if the ingredient exists
            var checkCommand = new MySqlCommand(
                "SELECT IngredientID FROM Ingredients WHERE Name = @Name");
            checkCommand.Parameters.AddWithValue("@Name", ingredientName);

            var result = _mySQLManager.ExecuteScalar(checkCommand);

            if (result != null) {
                return Convert.ToInt32(result);
            }

            // If not found, create new ingredient
            var insertCommand = new MySqlCommand(
                "INSERT INTO Ingredients (Name) VALUES (@Name); SELECT LAST_INSERT_ID();");
            insertCommand.Parameters.AddWithValue("@Name", ingredientName);

            result = _mySQLManager.ExecuteScalar(insertCommand);
            return Convert.ToInt32(result);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e) {
            // Increment counter
            ingredientCount++;

            // Create new Grid for ingredient row instead of StackPanel for better alignment
            Grid ingredientGrid = new Grid {
                Margin = new Thickness(0, 5, 0, 5),
                Name = $"ingredientGrid{ingredientCount}"
            };

            // Define columns to match the first ingredient's layout
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ingredientGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Create name label and textbox
            TextBlock nameLabel = new TextBlock {
                Text = "Name:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            TextBox nameTextBox = new TextBox {
                Name = $"txtIngredientName{ingredientCount}",
                Margin = new Thickness(0, 0, 10, 0),
            };

            // Create amount label and textbox
            TextBlock amountLabel = new TextBlock {
                Text = "Amount:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            TextBox amountTextBox = new TextBox {
                Name = $"txtIngredientAmount{ingredientCount}",
                Width = 60,
                Margin = new Thickness(0, 0, 5, 0),
            };

            // Create measure type combobox
            ComboBox measureTypeComboBox = new ComboBox {
                Name = $"cmbMeasureType{ingredientCount}",
                Width = 80,
                SelectedIndex = 0 // Default to Grams
            };

            // Add items to combobox
            measureTypeComboBox.Items.Add(new ComboBoxItem { Content = "Grams" });
            measureTypeComboBox.Items.Add(new ComboBoxItem { Content = "Pieces" });

            // Create remove button
            Button removeButton = new Button {
                Content = "✕",
                Width = 40,
                Height = 40,
                Margin = new Thickness(5, 0, 5, 0),
                ToolTip = "Remove ingredient",
                Tag = ingredientCount // Store the ingredient ID in the Tag for reference
            };
            // Add click handler for remove button
            removeButton.Click += RemoveIngredient_Click;

            // Apply styles from resource dictionary
            nameLabel.Style = FindResource("CroissantTextBlock") as Style;
            nameTextBox.Style = FindResource("CroissantTextBox") as Style;
            amountLabel.Style = FindResource("CroissantTextBlock") as Style;
            amountTextBox.Style = FindResource("CroissantTextBox") as Style;
            measureTypeComboBox.Style = FindResource("CroissantComboBox") as Style;

            // Try to apply a style to the button if it exists
            if (FindResource("CroissantButton") is Style buttonStyle)
                removeButton.Style = buttonStyle;

            // Add controls to the grid with column specifications
            Grid.SetColumn(nameLabel, 0);
            ingredientGrid.Children.Add(nameLabel);

            Grid.SetColumn(nameTextBox, 1);
            ingredientGrid.Children.Add(nameTextBox);

            Grid.SetColumn(amountLabel, 2);
            ingredientGrid.Children.Add(amountLabel);

            Grid.SetColumn(amountTextBox, 3);
            ingredientGrid.Children.Add(amountTextBox);

            Grid.SetColumn(measureTypeComboBox, 4);
            ingredientGrid.Children.Add(measureTypeComboBox);

            Grid.SetColumn(removeButton, 5);
            ingredientGrid.Children.Add(removeButton);

            // Insert the new row before the Add button
            ingredientsPanel.Children.Insert(ingredientsPanel.Children.Count - 1, ingredientGrid);
        }

        // Add this new method for removing ingredients
        private void RemoveIngredient_Click(object sender, RoutedEventArgs e) {
            // Get the button that was clicked
            Button button = sender as Button;
            if (button != null) {
                // Find the parent grid container (ingredient row)
                if (button.Parent is Grid ingredientGrid && ingredientGrid.Parent is StackPanel panel) {
                    // Remove the grid from the ingredients panel
                    panel.Children.Remove(ingredientGrid);
                }
            }
        }
    }
}