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
    ConsentTimestamp DATETIME NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
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
    ShowPhone BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    Complete BOOLEAN NOT NULL DEFAULT FALSE,
    Lease BOOLEAN NOT NULL DEFAULT FALSE,
    Approved BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Create Promotions table
CREATE TABLE IF NOT EXISTS Promotions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ListingId INT NOT NULL,
    ExpirationDate DATETIME NOT NULL,
    Category VARCHAR(100) NOT NULL,
    FOREIGN KEY (ListingId) REFERENCES Listings(Id)
);

-- Create indexes
CREATE INDEX IX_Listings_Category ON Listings(Category);
CREATE INDEX IX_Listings_UserId ON Listings(UserId);
CREATE INDEX IX_Listings_CreatedAt ON Listings(CreatedAt DESC);
CREATE INDEX IX_Promotions_Category ON Promotions(Category);
CREATE INDEX IX_Promotions_ExpirationDate ON Promotions(ExpirationDate);
EOSQL

# Apply freshest backup if exists
latest_backup=$(ls -1 /backups/maltalist_*.sql 2>/dev/null | sort | tail -n 1)
if [ -f "$latest_backup" ]; then
  mysql -u root -p${MYSQL_ROOT_PASSWORD} maltalist < "$latest_backup"
  echo "Backup $latest_backup applied successfully!"
fi

# Start backup script
chmod +x /backup.sh
nohup /backup.sh &

# Make sure the script was executed successfully
echo "Database initialization completed successfully!"
