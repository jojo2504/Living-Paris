USE LivingParis;

INSERT INTO Persons 
    (LastName, FirstName, Street, StreetNumber, Postcode, City, PhoneNumber, Mail, ClosestMetro)
VALUES  
    ("Dupond", "Marie", "Rue de la Republique", 30, "75011", "Paris", "1234567890", "Mdupond@gmail.com", "Republique");

INSERT INTO Chefs (PersonID)
SELECT PersonID 
FROM Persons 
WHERE PersonID BETWEEN LAST_INSERT_ID() AND LAST_INSERT_ID() + ROW_COUNT() - 1;


INSERT INTO Persons 
    (LastName, FirstName, Street, StreetNumber, Postcode, City, PhoneNumber, Mail, ClosestMetro)
VALUES  
    ("Durand", "Medhy", "Rue Cardinet", 15, "75017", "Paris", "1234567890", "Mdurand@gmail.com", "Cardinet");

INSERT INTO Clients (PersonID)
SELECT PersonID 
FROM Persons
WHERE PersonID BETWEEN LAST_INSERT_ID() AND LAST_INSERT_ID() + ROW_COUNT() - 1;


INSERT INTO Ingredients
    (Name) 
VALUES
    ("raclette fromage"), ("pomme de terre"), ("jambon"), ("cornichon"), ("fraise"), ("kiwi"), ("sucre");


INSERT INTO Orders
    (ChefID, ClientID)
VALUES
    (1, 1);


INSERT INTO Dishes (ChefID, Name, Type, DishPrice, FabricationDate, PeremptionDate, Diet, Origin)
VALUES
    (1, "raclette", "main dish", 10.00, '2025-01-10 00:00:00', '2025-01-15 00:00:00', NULL, "french"),
    (1, "salade de fruit", "dessert", 5.00, '2025-01-10 00:00:00', '2025-01-15 00:00:00', "vegetarian", "indifferent");

-- Insert Orders
INSERT INTO OrderDishes (DishID, OrderID, Quantity, OrderPrice)
VALUES
    ((SELECT DishID FROM Dishes WHERE Name = "raclette" AND ChefID = 1), 1, 6, 10.00),
    ((SELECT DishID FROM Dishes WHERE Name = "salade de fruit" AND ChefID = 1), 1, 6, 5.00);

INSERT INTO DishIngredients (IngredientID, DishID, Gramme, Pieces)
VALUES
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "raclette fromage"),
        (SELECT DishID FROM Dishes WHERE Name = "raclette" AND ChefID = 1),
        250, NULL
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "pomme de terre"),
        (SELECT DishID FROM Dishes WHERE Name = "raclette" AND ChefID = 1),
        200, NULL
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "jambon"),
        (SELECT DishID FROM Dishes WHERE Name = "raclette" AND ChefID = 1),
        200, NULL
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "cornichon"),
        (SELECT DishID FROM Dishes WHERE Name = "raclette" AND ChefID = 1),
        NULL, 3
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "fraise"),
        (SELECT DishID FROM Dishes WHERE Name = "salade de fruit" AND ChefID = 1),
        100, NULL
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "kiwi"),
        (SELECT DishID FROM Dishes WHERE Name = "salade de fruit" AND ChefID = 1),
        100, NULL
    ),
    (
        (SELECT IngredientID FROM Ingredients WHERE Name = "sucre"),
        (SELECT DishID FROM Dishes WHERE Name = "salade de fruit" AND ChefID = 1),
        10, NULL
    );

-- Return multiple result sets
SELECT * FROM Persons;
SELECT * FROM Chefs;
SELECT * FROM Clients;
SELECT * FROM DishIngredients;
SELECT * FROM Dishes;
SELECT * FROM Orders;
SELECT * FROM OrderDishes;