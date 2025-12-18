#!/bin/bash
set -e
set +H  # Disable history expansion to avoid issues with ! character in passwords

# Get the password from environment variable, with fallback for prod
MYSQL_PASSWORD=${MYSQL_PASSWORD:-M@LtApass}

mysql -u root -p${MYSQL_ROOT_PASSWORD} <<'EOSQL'
CREATE DATABASE IF NOT EXISTS maltalist;
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

# Create or update maltalist_user
# Use cat with a here-document to avoid shell interpretation issues with special characters
mysql -u root -p"${MYSQL_ROOT_PASSWORD}" -e "CREATE USER IF NOT EXISTS 'maltalist_user'@'%';" 2>/dev/null || true
mysql -u root -p"${MYSQL_ROOT_PASSWORD}" -e "GRANT ALL PRIVILEGES ON maltalist.* TO 'maltalist_user'@'%';"
# Set password safely using a here-document with single quotes
mysql -u root -p"${MYSQL_ROOT_PASSWORD}" << 'EOFPWD'
ALTER USER 'maltalist_user'@'%' IDENTIFIED WITH mysql_native_password BY 'PASSWORD_TO_REPLACE';
FLUSH PRIVILEGES;
EOFPWD
# This won't work because we can't use variables in single-quoted heredoc

# Alternative: Use printf to safely pass the SQL
printf "ALTER USER 'maltalist_user'@'%%' IDENTIFIED WITH mysql_native_password BY '%s';\nFLUSH PRIVILEGES;\n" "${MYSQL_PASSWORD}" | mysql -u root -p"${MYSQL_ROOT_PASSWORD}"

echo "Database and user created successfully!"

# Apply freshest backup if exists
latest_backup=$(ls -1 /backups/maltalist_*.sql 2>/dev/null | sort | tail -n 1)
if [ -f "$latest_backup" ]; then
  mysql -u root -p${MYSQL_ROOT_PASSWORD} maltalist < "$latest_backup"
  echo "Backup $latest_backup applied successfully!"
fi

# Check for prod flag or environment variable
IS_PROD=false
if [ "$1" = "--prod" ] || [ "$ENVIRONMENT" = "production" ]; then
    IS_PROD=true
fi

if [ "$IS_PROD" = false ]; then
    # Insert E2E test users AFTER backup restore to ensure they always exist
    echo "Inserting E2E test users..."
    mysql -u root -p${MYSQL_ROOT_PASSWORD} <<EOSQL
    USE maltalist;

    INSERT INTO Users (Id, UserName, Email, UserPicture, PhoneNumber, CreatedAt, LastOnline, ConsentTimestamp, IsActive)
    VALUES 
        (
            'e2e-test-user-1',
            'Test User One',
            'testuser1@maltalist.test',
            '/assets/img/users/test-user-1.jpg',
            '+356 2123 4567',
            NOW(),
            NOW(),
            NOW(),
            TRUE
        ),
        (
            'e2e-test-user-2',
            'Test User Two',
            'testuser2@maltalist.test',
            '/assets/img/users/test-user-2.jpg',
            '+356 2123 4568',
            NOW(),
            NOW(),
            NOW(),
            TRUE
        ),
        (
            'e2e-test-seller',
            'Test Seller',
            'seller@maltalist.test',
            '',
            '+356 9999 8888',
            NOW(),
            NOW(),
            NOW(),
            TRUE
        )
    ON DUPLICATE KEY UPDATE 
        UserName = VALUES(UserName),
        Email = VALUES(Email),
        UserPicture = VALUES(UserPicture),
        PhoneNumber = VALUES(PhoneNumber),
        IsActive = TRUE;
EOSQL
    echo "E2E test users ready!"
else
    echo "Production mode detected. Skipping E2E test users."
fi

# Start backup script
chmod +x /backup.sh
nohup /backup.sh &

# Make sure the script was executed successfully
echo "Database initialization completed successfully!"
