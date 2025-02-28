USE LivingParis;

-- Disable foreign key checks temporarily to avoid errors when dropping tables
SET FOREIGN_KEY_CHECKS = 0;

-- Drop tables in reverse order to avoid foreign key constraint errors
DROP TABLE IF EXISTS DishIngredients;
DROP TABLE IF EXISTS Volume;
DROP TABLE IF EXISTS Ingredients;
DROP TABLE IF EXISTS Dishes;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS OrderDishes;
DROP TABLE IF EXISTS Chefs;
DROP TABLE IF EXISTS Clients;
DROP TABLE IF EXISTS Persons;

-- Re-enable foreign key checks after dropping tables
SET FOREIGN_KEY_CHECKS = 1;