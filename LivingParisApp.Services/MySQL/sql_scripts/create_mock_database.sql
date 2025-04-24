-- SQL file to populate the LivingParis database

-- First, ensure we're using the correct database
USE LivingParis;

-- Clear existing data (if any)
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE OrderDishes;
TRUNCATE TABLE DishIngredients;
TRUNCATE TABLE Orders;
TRUNCATE TABLE Dishes;
TRUNCATE TABLE Ingredients;
TRUNCATE TABLE Users;
SET FOREIGN_KEY_CHECKS = 1;

-- 1. Insert Users (both clients and chefs)
INSERT INTO Users (LastName, FirstName, Street, StreetNumber, Postcode, City, PhoneNumber, Mail, ClosestMetro, Password, IsClient, IsChef) VALUES

-- Admin (1)
('Admin', 'Admin', 'Rue de Rivoli', 45, '75001', 'Paris', '1', 'admin', 'Louvre-Rivoli', 'admin', 1, 1),

-- Chefs (10)
('Dubois', 'Marie', 'Rue de Rivoli', 45, '75001', 'Paris', '1', 'marie.dubois@email.com', 'Louvre-Rivoli', 'chef_password1', 0, 1),
('Martin', 'Jean', 'Avenue des Champs-Élysées', 120, '75008', 'Paris', '2', 'jean.martin@email.com', 'George V', 'chef_password2', 0, 1),
('Leroy', 'Sophie', 'Boulevard Saint-Germain', 78, '75006', 'Paris', '3', 'sophie.leroy@email.com', 'Saint-Germain-des-Prés', 'chef_password3', 0, 1),
('Moreau', 'Pierre', 'Rue Montorgueil', 25, '75002', 'Paris', '4', 'pierre.moreau@email.com', 'Étienne Marcel', 'chef_password4', 0, 1),
('Bernard', 'Claire', 'Rue du Faubourg Saint-Honoré', 189, '75008', 'Paris', '5', 'claire.bernard@email.com', 'Miromesnil', 'chef_password5', 0, 1),
('Thomas', 'Michel', 'Avenue Montaigne', 32, '75008', 'Paris', '6', 'michel.thomas@email.com', 'Alma-Marceau', 'chef_password6', 0, 1),
('Petit', 'Isabelle', 'Rue de Charonne', 71, '75011', 'Paris', '7', 'isabelle.petit@email.com', 'Charonne', 'chef_password7', 0, 1),
('Robert', 'Antoine', 'Boulevard de Clichy', 56, '75018', 'Paris', '8', 'antoine.robert@email.com', 'Blanche', 'chef_password8', 0, 1),
('Richard', 'Nathalie', 'Rue de la Roquette', 103, '75011', 'Paris', '9', 'nathalie.richard@email.com', 'Bastille', 'chef_password9', 0, 1),
('Durand', 'François', 'Rue de Buci', 19, '75006', 'Paris', '0', 'francois.durand@email.com', 'Odéon', 'chef_password10', 0, 1),

-- Clients (10)
('Lambert', 'Julie', 'Rue Saint-Honoré', 235, '75001', 'Paris', '1', 'julie.lambert@email.com', 'Tuileries', 'client_password1', 1, 0),
('Simon', 'Thomas', 'Avenue de l''Opéra', 17, '75001', 'Paris', '2', 'thomas.simon@email.com', 'Opéra', 'client_password2', 1, 0),
('Roux', 'Caroline', 'Rue de Vaugirard', 150, '75015', 'Paris', '3', 'caroline.roux@email.com', 'Vaugirard', 'client_password3', 1, 0),
('Fournier', 'David', 'Rue de la Pompe', 42, '75016', 'Paris', '4', 'david.fournier@email.com', 'La Muette', 'client_password4', 1, 0),
('Morel', 'Amélie', 'Avenue Parmentier', 63, '75011', 'Paris', '5', 'amelie.morel@email.com', 'Parmentier', 'client_password5', 1, 0),
('Vincent', 'Nicolas', 'Rue des Martyrs', 88, '75009', 'Paris', '6', 'nicolas.vincent@email.com', 'Saint-Georges', 'client_password6', 1, 0),
('Legrand', 'Stéphanie', 'Boulevard Raspail', 112, '75006', 'Paris', '7', 'stephanie.legrand@email.com', 'Rennes', 'client_password7', 1, 0),
('Rousseau', 'Julien', 'Avenue de Clichy', 35, '75017', 'Paris', '8', 'julien.rousseau@email.com', 'La Fourche', 'client_password8', 1, 0),
('Mercier', 'Elise', 'Rue de Belleville', 128, '75020', 'Paris', '9', 'elise.mercier@email.com', 'Jourdain', 'client_password9', 1, 0),
('Garcia', 'Paul', 'Rue Oberkampf', 72, '75011', 'Paris', '0', 'paul.garcia@email.com', 'Parmentier', 'client_password10', 1, 0);

-- 2. Insert Ingredients
INSERT INTO Ingredients (Name) VALUES
('Flour'),
('Butter'),
('Sugar'),
('Eggs'),
('Salt'),
('Olive Oil'),
('Garlic'),
('Onion'),
('Tomato'),
('Carrot'),
('Potato'),
('Beef'),
('Chicken'),
('Salmon'),
('Rice'),
('Pasta'),
('Lemon'),
('Basil'),
('Thyme'),
('Rosemary'),
('Chocolate'),
('Vanilla'),
('Cinnamon'),
('Milk'),
('Cream'),
('Cheese'),
('Mushroom'),
('Bell Pepper'),
('Zucchini'),
('Eggplant'),
('Spinach'),
('Lettuce'),
('Cucumber'),
('Avocado'),
('Shrimp'),
('Mustard'),
('Vinegar'),
('Honey'),
('Soy Sauce'),
('Ginger');

-- 3. Insert Dishes (for each Chef)
INSERT INTO Dishes (ChefID, Name, Type, DishPrice, FabricationDate, PeremptionDate, Diet, Origin) VALUES
-- Chef 1: Marie Dubois
(1, 'Quiche Lorraine', 'entree', 12.50, '2025-04-15 09:00:00', '2025-04-18 09:00:00', 'Non-Vegetarian', 'French'),
(1, 'Coq au Vin', 'main dish', 18.90, '2025-04-15 10:30:00', '2025-04-18 10:30:00', 'Non-Vegetarian', 'French'),
(1, 'Crème Brûlée', 'dessert', 9.75, '2025-04-15 14:00:00', '2025-04-19 14:00:00', 'Vegetarian', 'French'),

-- Chef 2: Jean Martin
(2, 'Foie Gras Terrine', 'entree', 15.00, '2025-04-14 08:00:00', '2025-04-17 08:00:00', 'Non-Vegetarian', 'French'),
(2, 'Beef Bourguignon', 'main dish', 19.50, '2025-04-14 11:00:00', '2025-04-17 11:00:00', 'Non-Vegetarian', 'French'),
(2, 'Tarte Tatin', 'dessert', 8.95, '2025-04-14 15:30:00', '2025-04-19 15:30:00', 'Vegetarian', 'French'),

-- Chef 3: Sophie Leroy
(3, 'Niçoise Salad', 'entree', 11.25, '2025-04-15 09:15:00', '2025-04-18 09:15:00', 'Pescatarian', 'French'),
(3, 'Ratatouille', 'main dish', 16.50, '2025-04-15 12:00:00', '2025-04-18 12:00:00', 'Vegan', 'French'),
(3, 'Mousse au Chocolat', 'dessert', 7.95, '2025-04-15 14:30:00', '2025-04-19 14:30:00', 'Vegetarian', 'French'),

-- Chef 4: Pierre Moreau
(4, 'Escargots de Bourgogne', 'entree', 14.75, '2025-04-14 09:30:00', '2025-04-17 09:30:00', 'Non-Vegetarian', 'French'),
(4, 'Bouillabaisse', 'main dish', 22.50, '2025-04-14 12:30:00', '2025-04-17 12:30:00', 'Pescatarian', 'French'),
(4, 'Paris-Brest', 'dessert', 10.50, '2025-04-14 16:00:00', '2025-04-18 16:00:00', 'Vegetarian', 'French'),

-- Chef 5: Claire Bernard
(5, 'French Onion Soup', 'entree', 9.95, '2025-04-15 08:45:00', '2025-04-18 08:45:00', 'Vegetarian', 'French'),
(5, 'Cassoulet', 'main dish', 17.95, '2025-04-15 11:30:00', '2025-04-18 11:30:00', 'Non-Vegetarian', 'French'),
(5, 'Mille-feuille', 'dessert', 8.50, '2025-04-15 15:00:00', '2025-04-19 15:00:00', 'Vegetarian', 'French'),

-- Chef 6: Michel Thomas
(6, 'Pâté de Campagne', 'entree', 13.25, '2025-04-14 09:00:00', '2025-04-17 09:00:00', 'Non-Vegetarian', 'French'),
(6, 'Blanquette de Veau', 'main dish', 18.75, '2025-04-14 12:00:00', '2025-04-17 12:00:00', 'Non-Vegetarian', 'French'),
(6, 'Tarte au Citron', 'dessert', 9.25, '2025-04-14 15:00:00', '2025-04-19 15:00:00', 'Vegetarian', 'French'),

-- Chef 7: Isabelle Petit
(7, 'Soupe à l''Oignon', 'entree', 10.50, '2025-04-15 08:30:00', '2025-04-18 08:30:00', 'Vegetarian', 'French'),
(7, 'Confit de Canard', 'main dish', 21.00, '2025-04-15 11:00:00', '2025-04-18 11:00:00', 'Non-Vegetarian', 'French'),
(7, 'Profiteroles', 'dessert', 9.95, '2025-04-15 14:45:00', '2025-04-19 14:45:00', 'Vegetarian', 'French'),

-- Chef 8: Antoine Robert
(8, 'Salade Lyonnaise', 'entree', 12.95, '2025-04-14 09:15:00', '2025-04-17 09:15:00', 'Non-Vegetarian', 'French'),
(8, 'Pot-au-Feu', 'main dish', 17.50, '2025-04-14 11:45:00', '2025-04-17 11:45:00', 'Non-Vegetarian', 'French'),
(8, 'Éclair au Chocolat', 'dessert', 7.50, '2025-04-14 15:15:00', '2025-04-19 15:15:00', 'Vegetarian', 'French'),

-- Chef 9: Nathalie Richard
(9, 'Gougères', 'entree', 11.00, '2025-04-15 09:30:00', '2025-04-18 09:30:00', 'Vegetarian', 'French'),
(9, 'Sole Meunière', 'main dish', 23.50, '2025-04-15 12:15:00', '2025-04-18 12:15:00', 'Pescatarian', 'French'),
(9, 'Crêpes Suzette', 'dessert', 10.95, '2025-04-15 15:30:00', '2025-04-19 15:30:00', 'Vegetarian', 'French'),

-- Chef 10: François Durand
(10, 'Tartare de Saumon', 'entree', 14.50, '2025-04-14 09:45:00', '2025-04-17 09:45:00', 'Pescatarian', 'French'),
(10, 'Boeuf en Croûte', 'main dish', 25.00, '2025-04-14 11:15:00', '2025-04-17 11:15:00', 'Non-Vegetarian', 'French'),
(10, 'Opera Cake', 'dessert', 11.25, '2025-04-14 15:45:00', '2025-04-19 15:45:00', 'Vegetarian', 'French');

-- 4. Insert DishIngredients
INSERT INTO DishIngredients (IngredientID, DishID, Gramme, Pieces) VALUES
-- Quiche Lorraine
(1, 1, 250, NULL), -- Flour
(2, 1, 150, NULL), -- Butter
(4, 1, NULL, 3),   -- Eggs
(5, 1, 5, NULL),   -- Salt
(24, 1, 200, NULL), -- Milk
(26, 1, 150, NULL), -- Cheese

-- Coq au Vin
(13, 2, 500, NULL), -- Chicken
(2, 2, 50, NULL),   -- Butter
(7, 2, NULL, 3),    -- Garlic
(8, 2, NULL, 1),    -- Onion
(19, 2, 5, NULL),   -- Thyme
(20, 2, 5, NULL),   -- Rosemary

-- Crème Brûlée
(4, 3, NULL, 4),    -- Eggs
(3, 3, 100, NULL),  -- Sugar
(25, 3, 500, NULL), -- Cream
(22, 3, 10, NULL),  -- Vanilla

-- Foie Gras Terrine
(2, 4, 30, NULL),   -- Butter
(5, 4, 5, NULL),    -- Salt
(17, 4, NULL, 1),   -- Lemon
(19, 4, 3, NULL),   -- Thyme

-- Beef Bourguignon
(12, 5, 800, NULL), -- Beef
(6, 5, 30, NULL),   -- Olive Oil
(7, 5, NULL, 3),    -- Garlic
(8, 5, NULL, 2),    -- Onion
(10, 5, NULL, 2),   -- Carrot
(27, 5, 200, NULL), -- Mushroom

-- Tarte Tatin
(1, 6, 200, NULL),  -- Flour
(2, 6, 100, NULL),  -- Butter
(3, 6, 150, NULL),  -- Sugar
(4, 6, NULL, 1);    -- Eggs

-- Continue with more DishIngredients for remaining dishes...

-- 5. Insert Orders
INSERT INTO Orders (ClientID, ChefID, Address, OrderDate, OrderTotal, Status) VALUES
(11, 1, '235 Rue Saint-Honoré, 75001 Paris', '2025-04-01', 41.15, 'Completed'),
(12, 2, '17 Avenue de l''Opéra, 75001 Paris', '2025-04-02', 43.45, 'Completed'),
(13, 3, '150 Rue de Vaugirard, 75015 Paris', '2025-04-03', 35.70, 'Completed'),
(14, 4, '42 Rue de la Pompe, 75016 Paris', '2025-04-04', 47.75, 'Completed'),
(15, 5, '63 Avenue Parmentier, 75011 Paris', '2025-04-05', 36.40, 'Completed'),
(16, 6, '88 Rue des Martyrs, 75009 Paris', '2025-04-06', 41.25, 'Completed'),
(17, 7, '112 Boulevard Raspail, 75006 Paris', '2025-04-07', 41.45, 'Completed'),
(18, 8, '35 Avenue de Clichy, 75017 Paris', '2025-04-08', 37.95, 'Completed'),
(19, 9, '128 Rue de Belleville, 75020 Paris', '2025-04-09', 45.45, 'Completed'),
(20, 10, '72 Rue Oberkampf, 75011 Paris', '2025-04-10', 50.75, 'Completed'),
(11, 3, '235 Rue Saint-Honoré, 75001 Paris', '2025-04-11', 35.70, 'Completed'),
(12, 4, '17 Avenue de l''Opéra, 75001 Paris', '2025-04-12', 47.75, 'Completed'),
(13, 5, '150 Rue de Vaugirard, 75015 Paris', '2025-04-13', 36.40, 'Completed'),
(14, 6, '42 Rue de la Pompe, 75016 Paris', '2025-04-14', 41.25, 'Completed'),
(15, 7, '63 Avenue Parmentier, 75011 Paris', '2025-04-15', 41.45, 'Pending'),
(16, 8, '88 Rue des Martyrs, 75009 Paris', '2025-04-15', 37.95, 'Pending'),
(17, 9, '112 Boulevard Raspail, 75006 Paris', '2025-04-16', 45.45, 'Pending'),
(18, 10, '35 Avenue de Clichy, 75017 Paris', '2025-04-16', 50.75, 'Pending'),
(19, 1, '128 Rue de Belleville, 75020 Paris', '2025-04-16', 41.15, 'Pending'),
(20, 2, '72 Rue Oberkampf, 75011 Paris', '2025-04-16', 43.45, 'Pending');

-- 6. Insert OrderDishes
INSERT INTO OrderDishes (OrderID, DishID, Quantity, OrderPrice) VALUES
-- Order 1
(1, 1, 1, 12.50),  -- Quiche Lorraine
(1, 2, 1, 18.90),  -- Coq au Vin
(1, 3, 1, 9.75),   -- Crème Brûlée

-- Order 2
(2, 4, 1, 15.00),  -- Foie Gras Terrine
(2, 5, 1, 19.50),  -- Beef Bourguignon
(2, 6, 1, 8.95),   -- Tarte Tatin

-- Order 3
(3, 7, 1, 11.25),  -- Niçoise Salad
(3, 8, 1, 16.50),  -- Ratatouille
(3, 9, 1, 7.95),   -- Mousse au Chocolat

-- Order 4
(4, 10, 1, 14.75), -- Escargots de Bourgogne
(4, 11, 1, 22.50), -- Bouillabaisse
(4, 12, 1, 10.50), -- Paris-Brest

-- Order 5
(5, 13, 1, 9.95),  -- French Onion Soup
(5, 14, 1, 17.95), -- Cassoulet
(5, 15, 1, 8.50),  -- Mille-feuille

-- Order 6
(6, 16, 1, 13.25), -- Pâté de Campagne
(6, 17, 1, 18.75), -- Blanquette de Veau
(6, 18, 1, 9.25),  -- Tarte au Citron

-- Order 7
(7, 19, 1, 10.50), -- Soupe à l'Oignon
(7, 20, 1, 21.00), -- Confit de Canard
(7, 21, 1, 9.95),  -- Profiteroles

-- Order 8
(8, 22, 1, 12.95), -- Salade Lyonnaise
(8, 23, 1, 17.50), -- Pot-au-Feu
(8, 24, 1, 7.50),  -- Éclair au Chocolat

-- Order 9
(9, 25, 1, 11.00), -- Gougères
(9, 26, 1, 23.50), -- Sole Meunière
(9, 27, 1, 10.95), -- Crêpes Suzette

-- Order 10
(10, 28, 1, 14.50), -- Tartare de Saumon
(10, 29, 1, 25.00), -- Boeuf en Croûte
(10, 30, 1, 11.25), -- Opera Cake

-- Order 11
(11, 7, 1, 11.25), -- Niçoise Salad
(11, 8, 1, 16.50), -- Ratatouille
(11, 9, 1, 7.95),  -- Mousse au Chocolat

-- Order 12
(12, 10, 1, 14.75), -- Escargots de Bourgogne
(12, 11, 1, 22.50), -- Bouillabaisse
(12, 12, 1, 10.50), -- Paris-Brest

-- Order 13
(13, 13, 1, 9.95),  -- French Onion Soup
(13, 14, 1, 17.95), -- Cassoulet
(13, 15, 1, 8.50),  -- Mille-feuille

-- Order 14
(14, 16, 1, 13.25), -- Pâté de Campagne
(14, 17, 1, 18.75), -- Blanquette de Veau
(14, 18, 1, 9.25),  -- Tarte au Citron

-- Order 15
(15, 19, 1, 10.50), -- Soupe à l'Oignon
(15, 20, 1, 21.00), -- Confit de Canard
(15, 21, 1, 9.95),  -- Profiteroles

-- Order 16
(16, 22, 1, 12.95), -- Salade Lyonnaise
(16, 23, 1, 17.50), -- Pot-au-Feu
(16, 24, 1, 7.50),  -- Éclair au Chocolat

-- Order 17
(17, 25, 1, 11.00), -- Gougères
(17, 26, 1, 23.50), -- Sole Meunière
(17, 27, 1, 10.95), -- Crêpes Suzette

-- Order 18
(18, 28, 1, 14.50), -- Tartare de Saumon
(18, 29, 1, 25.00), -- Boeuf en Croûte
(18, 30, 1, 11.25), -- Opera Cake

-- Order 19
(19, 1, 1, 12.50),  -- Quiche Lorraine
(19, 2, 1, 18.90),  -- Coq au Vin
(19, 3, 1, 9.75),   -- Crème Brûlée

-- Order 20
(20, 4, 1, 15.00),  -- Foie Gras Terrine
(20, 5, 1, 19.50),  -- Beef Bourguignon
(20, 6, 1, 8.95);   -- Tarte Tatin

-- Add more DishIngredients for the remaining dishes
-- Adding a few more examples
INSERT INTO DishIngredients (IngredientID, DishID, Gramme, Pieces) VALUES
-- Ratatouille
(6, 8, 40, NULL),    -- Olive Oil
(7, 8, NULL, 4),     -- Garlic
(8, 8, NULL, 1),     -- Onion
(9, 8, NULL, 4),     -- Tomato
(28, 8, NULL, 2),    -- Bell Pepper
(29, 8, NULL, 2),    -- Zucchini
(30, 8, NULL, 1),    -- Eggplant
(18, 8, 15, NULL),   -- Basil

-- Mousse au Chocolat
(21, 9, 200, NULL),  -- Chocolate
(4, 9, NULL, 6),     -- Eggs
(3, 9, 50, NULL),    -- Sugar
(25, 9, 100, NULL),  -- Cream

-- Bouillabaisse
(6, 11, 30, NULL),   -- Olive Oil
(7, 11, NULL, 3),    -- Garlic
(8, 11, NULL, 1),    -- Onion
(9, 11, NULL, 2),    -- Tomato
(14, 11, 300, NULL), -- Salmon
(35, 11, 200, NULL), -- Shrimp
(5, 11, 10, NULL),   -- Salt
(18, 11, 5, NULL);   -- Basil

-- And so on for other dishes...

-- Complete the database with more extensive ingredient lists as needed