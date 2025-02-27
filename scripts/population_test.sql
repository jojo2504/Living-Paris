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


INSERT INTO Ingredients ()


-- Return multiple result sets
SELECT * FROM Persons;
SELECT * FROM Chefs;
SELECT * FROM Clients;