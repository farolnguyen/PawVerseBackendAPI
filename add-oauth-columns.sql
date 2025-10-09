-- ===================================================================
-- ADD OAUTH REFRESH TOKEN COLUMNS TO AspNetUsers
-- Run this in SQL Server Management Studio or Azure Data Studio
-- ===================================================================

USE PawVerse;
GO

-- Check if columns already exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'RefreshToken')
BEGIN
    ALTER TABLE AspNetUsers
    ADD RefreshToken NVARCHAR(MAX) NULL;
    
    PRINT 'Added RefreshToken column';
END
ELSE
BEGIN
    PRINT 'RefreshToken column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'RefreshTokenExpiryTime')
BEGIN
    ALTER TABLE AspNetUsers
    ADD RefreshTokenExpiryTime DATETIME2 NULL;
    
    PRINT 'Added RefreshTokenExpiryTime column';
END
ELSE
BEGIN
    PRINT 'RefreshTokenExpiryTime column already exists';
END
GO

-- Verify columns were added
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
  AND COLUMN_NAME IN ('RefreshToken', 'RefreshTokenExpiryTime');
GO

PRINT 'Done! Columns added successfully.';
