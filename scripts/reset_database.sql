CREATE DATABASE IF NOT EXISTS LivingParis;
USE LivingParis;

-- Disable foreign key checks temporarily to avoid errors when dropping tables
SET FOREIGN_KEY_CHECKS = 0;

-- Drop tables in reverse order to avoid foreign key constraint errors
DROP TABLE IF EXISTS dishes;
DROP TABLE IF EXISTS dishingredients;
DROP TABLE IF EXISTS ingredients;
DROP TABLE IF EXISTS orderdishes;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS users;

-- Re-enable foreign key checks after dropping tables
SET FOREIGN_KEY_CHECKS = 1;