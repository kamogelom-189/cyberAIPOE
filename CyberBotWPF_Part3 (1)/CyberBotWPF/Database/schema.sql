-- =====================================================================
-- CyberBot WPF — Database Schema
-- Run this once against your MySQL server (e.g. via MySQL Workbench,
-- phpMyAdmin, or the mysql CLI) before launching the app. The app's
-- "Database" tab can also auto-create these tables for you at runtime
-- (DatabaseHelper.InitializeSchema), but running this script manually
-- is recommended for first-time setup.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS cyberbot_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE cyberbot_db;

-- ---------------------------------------------------------------------
-- Tasks: Cybersecurity Task Assistant
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Tasks (
    TaskID        INT AUTO_INCREMENT PRIMARY KEY,
    Title         VARCHAR(200)  NOT NULL,
    Description   TEXT          NULL,
    ReminderDate  DATETIME      NULL,
    IsCompleted   BOOLEAN       NOT NULL DEFAULT 0,
    DateCreated   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ---------------------------------------------------------------------
-- ActivityLog: records every important action taken in the app
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ActivityLog (
    LogID      INT AUTO_INCREMENT PRIMARY KEY,
    Timestamp  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Action     VARCHAR(100) NOT NULL,
    Details    VARCHAR(500) NULL
);

-- ---------------------------------------------------------------------
-- Optional sample data (safe to delete) — illustrates the example
-- tasks mentioned in the assignment brief.
-- ---------------------------------------------------------------------
INSERT INTO Tasks (Title, Description, ReminderDate, IsCompleted) VALUES
 ('Enable Two-Factor Authentication', 'Turn on 2FA for email and banking accounts.', CURDATE() + INTERVAL 1 DAY, 0),
 ('Review Privacy Settings',          'Check social media privacy & app permissions.', CURDATE() + INTERVAL 3 DAY, 0),
 ('Update Password',                  'Change password on accounts older than 6 months.', CURDATE(), 0),
 ('Backup Important Files',           'Run the weekly 3-2-1 backup routine.', NULL, 1),
 ('Scan Computer for Malware',        'Run a full Windows Defender / Malwarebytes scan.', NULL, 0);
