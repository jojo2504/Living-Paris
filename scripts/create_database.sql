CREATE DATABASE IF NOT EXISTS LivingParis;

USE LivingParis;

CREATE TABLE Persons (
    PersonID INT AUTO_INCREMENT NOT NULL,
    LastName VARCHAR(255) NOT NULL,
    FirstName VARCHAR(255) NOT NULL,
    Street VARCHAR(255) NOT NULL,
    StreetNumber INT NOT NULL,
    Postcode VARCHAR(5) NOT NULL,
    City VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(10) NOT NULL,
    Mail VARCHAR(255) NOT NULL UNIQUE,
    ClosestMetro VARCHAR(255) NOT NULL,
    PRIMARY KEY (PersonID),
    CHECK (CHAR_LENGTH(PhoneNumber) = 10)
);

CREATE TABLE Clients (
    ClientID int AUTO_INCREMENT NOT NULL,
    PersonID int NOT NULL,
    PRIMARY KEY (ClientID),
    FOREIGN KEY (PersonID) REFERENCES Persons(PersonID)
);

CREATE TABLE Chefs (
    ChefID int AUTO_INCREMENT NOT NULL,
    PersonID int NOT NULL,
    PRIMARY KEY (ChefID),
    FOREIGN KEY (PersonID) REFERENCES Persons(PersonID)
);

CREATE TABLE Orders (
    OrderID INT AUTO_INCREMENT NOT NULL,
    ChefID INT NOT NULL,
    ClientID INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Quantity INT NOT NULL,

    PRIMARY KEY (OrderID),
    FOREIGN KEY (ChefID) REFERENCES Chefs(ChefID),
    FOREIGN KEY (ClientID) REFERENCES Clients(ClientID)
);  

CREATE TABLE Dishes (
    DishID INT AUTO_INCREMENT NOT NULL,
    ChefID INT NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Type VARCHAR(255) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    FabricationDate DATETIME NOT NULL,
    PeremptionDate DATETIME NOT NULL,
    Diet VARCHAR(255),
    Origin VARCHAR(255),
    PRIMARY KEY (DishID),
    FOREIGN KEY (ChefID) REFERENCES Chefs(ChefID),
    CHECK (Type IN ('entree', 'main dish', 'dessert'))
);

CREATE TABLE Ingredients (
    IngredientID INT AUTO_INCREMENT NOT NULL,
    Name VARCHAR(255) NOT NULL UNIQUE,
    PRIMARY KEY (IngredientID)
);

CREATE TABLE Volume (
    VolumeID INT AUTO_INCREMENT NOT NULL,
    Gramme INT DEFAULT NULL,
    Pieces INT DEFAULT NULL,
    PRIMARY KEY (VolumeID),
    CHECK (
        (Gramme IS NOT NULL AND Pieces IS NULL) OR
        (Pieces IS NOT NULL AND Gramme IS NULL)
    )
);

CREATE TABLE DishIngredients(
    DishIngredientsID INT AUTO_INCREMENT NOT NULL,
    IngredientID INT NOT NULL,
    VolumeID INT NOT NULL,
    DishID INT NOT NULL,
    PRIMARY KEY (DishIngredientsID),
    FOREIGN KEY (IngredientID) REFERENCES Ingredients(IngredientID),
    FOREIGN KEY (VolumeID) REFERENCES Volume(VolumeID),
    FOREIGN KEY (DishID) REFERENCES Dishes(DishID)
);

CREATE TRIGGER validate_email_format
BEFORE INSERT ON Persons
FOR EACH ROW
BEGIN
    IF NOT (NEW.Mail REGEXP '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Invalid email format';
    END IF;
END