CREATE DATABASE IF NOT EXISTS LivingParis;

USE LivingParis;

-- Users TABLE IF NOT EXISTS (base TABLE IF NOT EXISTS, no dependencies)
CREATE TABLE IF NOT EXISTS Users (
    UserID INT AUTO_INCREMENT NOT NULL,
    LastName VARCHAR(255) NOT NULL,
    FirstName VARCHAR(255) NOT NULL,
    Street VARCHAR(255) NOT NULL,
    StreetNumber INT NOT NULL,
    Postcode VARCHAR(5) NOT NULL,
    City VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(10) NOT NULL,
    Mail VARCHAR(255) NOT NULL UNIQUE,
    ClosestMetro VARCHAR(255) NOT NULL,
    Password VARCHAR(255) NOT NULL,
    IsClient INT NOT NULL DEFAULT 0,
    IsChef INT NOT NULL DEFAULT 0,
    PRIMARY KEY (UserID),
    CHECK (CHAR_LENGTH(PhoneNumber) = 1)
);

-- Orders depends on Chefs and Clients and Dishes
CREATE TABLE IF NOT EXISTS Orders (
	OrderID INT AUTO_INCREMENT NOT NULL,
    ClientID INT NOT NULL,
    ChefID INT NOT NULL,
    Address VARCHAR(255),
    OrderDate DATE,
    OrderTotal DECIMAL(10,2),
    PRIMARY KEY (OrderID),
    FOREIGN KEY (ClientId) REFERENCES Users(UserID),
    FOREIGN KEY (ChefId) REFERENCES Users(UserID)
);

-- Dishes depends on Chefs
CREATE TABLE IF NOT EXISTS Dishes (
    DishID INT AUTO_INCREMENT NOT NULL,
    ChefID INT NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Type VARCHAR(255) NOT NULL,
    DishPrice DECIMAL(10,2) NOT NULL,
    FabricationDate DATETIME NOT NULL,
    PeremptionDate DATETIME NOT NULL,
    Diet VARCHAR(255),
    Origin VARCHAR(255),
    PRIMARY KEY (DishID),
    FOREIGN KEY (ChefID) REFERENCES Users(UserID),
    CHECK (Type IN ('entree', 'main dish', 'dessert')),
    CHECK (DishPrice >= 0)
);

CREATE TABLE IF NOT EXISTS OrderDishes (
    DishID INT NOT NULL,
    OrderID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    OrderPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
    PRIMARY KEY (DishID,OrderID),
    FOREIGN KEY (DishID) REFERENCES Dishes(DishID) ON DELETE CASCADE,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    CHECK (Quantity > 0)
);

-- Ingredients (base TABLE IF NOT EXISTS, no dependencies)
CREATE TABLE IF NOT EXISTS Ingredients (
    IngredientID INT AUTO_INCREMENT NOT NULL,
    Name VARCHAR(255) NOT NULL UNIQUE,
    PRIMARY KEY (IngredientID)
);

-- DishIngredients depends on Dishes and Ingredients
CREATE TABLE IF NOT EXISTS DishIngredients (
    DishIngredientsID INT AUTO_INCREMENT NOT NULL,
    IngredientID INT NOT NULL,
    DishID INT NOT NULL,
    Gramme INT DEFAULT NULL,
    Pieces INT DEFAULT NULL,
    PRIMARY KEY (DishIngredientsID, DishID),
    FOREIGN KEY (IngredientID) REFERENCES Ingredients(IngredientID),
    FOREIGN KEY (DishID) REFERENCES Dishes(DishID),

    CHECK (
        (Gramme IS NOT NULL AND Pieces IS NULL) OR
        (Pieces IS NOT NULL AND Gramme IS NULL)
    )
);