#!/bin/bash
set -e

mysql -u root -p${MYSQL_ROOT_PASSWORD} <<EOSQL
USE maltalist;

-- Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id VARCHAR(255) PRIMARY KEY,
    UserName VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(50),
    UserPicture TEXT NOT NULL,
    Email VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    LastOnline DATETIME NOT NULL,
    UNIQUE KEY UK_Email (Email)
);

-- Create Listings table
CREATE TABLE IF NOT EXISTS Listings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Category VARCHAR(100) NOT NULL,
    Location VARCHAR(255) NOT NULL,
    UserId VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Create indexes
CREATE INDEX IX_Listings_Category ON Listings(Category);
CREATE INDEX IX_Listings_UserId ON Listings(UserId);
CREATE INDEX IX_Listings_CreatedAt ON Listings(CreatedAt DESC);
EOSQL

# Make sure the script was executed successfully
echo "Database initialization completed successfully!"
