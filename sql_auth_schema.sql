-- =============================================
-- SQL-скрипт для создания таблиц модуля аутентификации
-- Платформа: BotConstructor
-- Версия: 1.0
-- Дата: 12.02.2026
-- =============================================
--
-- Разделы из ТЗ:
-- 1.1 Email/Password аутентификация пользователей
-- 1.2 Админка для управления пользователями
-- 1.3 Личный кабинет пользователя
--
-- =============================================

-- Использование базы данных
USE BotConstructorDB;

-- =============================================
-- Таблица: Roles
-- Описание: Роли пользователей в системе
-- =============================================
CREATE TABLE IF NOT EXISTS `Roles` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(50) NOT NULL UNIQUE,
    `Description` VARCHAR(255) NULL,
    `IsSystemRole` BOOLEAN NOT NULL DEFAULT FALSE,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    INDEX `IX_Roles_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Роли пользователей (SuperAdmin, Admin, User, Developer, Affiliate)';

-- =============================================
-- Таблица: Users
-- Описание: Основная таблица пользователей
-- =============================================
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Email` VARCHAR(255) NOT NULL UNIQUE,
    `PasswordHash` VARCHAR(255) NOT NULL COMMENT 'BCrypt хеш пароля (cost factor 12)',
    `FirstName` VARCHAR(100) NULL,
    `LastName` VARCHAR(100) NULL,
    `EmailConfirmed` BOOLEAN NOT NULL DEFAULT FALSE,
    `EmailConfirmedAt` DATETIME NULL,
    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,
    `IsBlocked` BOOLEAN NOT NULL DEFAULT FALSE,
    `BlockedAt` DATETIME NULL,
    `BlockedReason` VARCHAR(500) NULL,
    `LastLoginAt` DATETIME NULL,
    `LastLoginIp` VARCHAR(45) NULL COMMENT 'IP адрес последнего входа (поддержка IPv6)',
    `FailedLoginAttempts` INT NOT NULL DEFAULT 0,
    `LockedOutUntil` DATETIME NULL COMMENT 'Временная блокировка при превышении попыток входа',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    INDEX `IX_Users_Email` (`Email`),
    INDEX `IX_Users_IsActive` (`IsActive`),
    INDEX `IX_Users_IsBlocked` (`IsBlocked`),
    INDEX `IX_Users_EmailConfirmed` (`EmailConfirmed`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Пользователи платформы';

-- =============================================
-- Таблица: UserRoles
-- Описание: Связь между пользователями и ролями (многие ко многим)
-- =============================================
CREATE TABLE IF NOT EXISTS `UserRoles` (
    `UserId` INT NOT NULL,
    `RoleId` INT NOT NULL,
    `AssignedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_UserRoles_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Roles`
        FOREIGN KEY (`RoleId`) REFERENCES `Roles`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_UserRoles_UserId` (`UserId`),
    INDEX `IX_UserRoles_RoleId` (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Связь пользователей с ролями';

-- =============================================
-- Таблица: LoginAttempts
-- Описание: Логирование всех попыток входа (успешных и неудачных)
-- =============================================
CREATE TABLE IF NOT EXISTS `LoginAttempts` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NULL COMMENT 'NULL если пользователь не найден',
    `Email` VARCHAR(255) NOT NULL,
    `IsSuccessful` BOOLEAN NOT NULL DEFAULT FALSE,
    `IpAddress` VARCHAR(45) NOT NULL,
    `UserAgent` VARCHAR(500) NULL,
    `FailureReason` VARCHAR(500) NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_LoginAttempts_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_LoginAttempts_UserId` (`UserId`),
    INDEX `IX_LoginAttempts_Email` (`Email`),
    INDEX `IX_LoginAttempts_IpAddress` (`IpAddress`),
    INDEX `IX_LoginAttempts_CreatedAt` (`CreatedAt` DESC),
    INDEX `IX_LoginAttempts_IsSuccessful` (`IsSuccessful`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Аудит попыток входа в систему';

-- =============================================
-- Таблица: PasswordResetTokens
-- Описание: Токены для сброса пароля
-- =============================================
CREATE TABLE IF NOT EXISTS `PasswordResetTokens` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `Token` VARCHAR(100) NOT NULL UNIQUE COMMENT 'Уникальный токен для сброса пароля',
    `ExpiresAt` DATETIME NOT NULL COMMENT 'Время истечения токена (обычно 1 час)',
    `IsUsed` BOOLEAN NOT NULL DEFAULT FALSE,
    `UsedAt` DATETIME NULL,
    `IpAddress` VARCHAR(45) NULL COMMENT 'IP адрес при использовании токена',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_PasswordResetTokens_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_PasswordResetTokens_UserId` (`UserId`),
    INDEX `IX_PasswordResetTokens_Token` (`Token`),
    INDEX `IX_PasswordResetTokens_ExpiresAt` (`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Токены для восстановления пароля';

-- =============================================
-- Таблица: EmailConfirmationTokens
-- Описание: Токены для подтверждения email
-- =============================================
CREATE TABLE IF NOT EXISTS `EmailConfirmationTokens` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `Token` VARCHAR(100) NOT NULL UNIQUE COMMENT 'Уникальный токен для подтверждения email',
    `ExpiresAt` DATETIME NOT NULL COMMENT 'Время истечения токена (обычно 24 часа)',
    `IsUsed` BOOLEAN NOT NULL DEFAULT FALSE,
    `UsedAt` DATETIME NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_EmailConfirmationTokens_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_EmailConfirmationTokens_UserId` (`UserId`),
    INDEX `IX_EmailConfirmationTokens_Token` (`Token`),
    INDEX `IX_EmailConfirmationTokens_ExpiresAt` (`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Токены для подтверждения email адреса';

-- =============================================
-- Таблица: UserSessions
-- Описание: Активные сессии пользователей
-- =============================================
CREATE TABLE IF NOT EXISTS `UserSessions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `SessionId` VARCHAR(255) NOT NULL UNIQUE COMMENT 'Уникальный идентификатор сессии',
    `IpAddress` VARCHAR(45) NOT NULL,
    `UserAgent` VARCHAR(500) NULL,
    `LastActivityAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ExpiresAt` DATETIME NOT NULL COMMENT 'Время истечения сессии',
    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_UserSessions_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_UserSessions_UserId` (`UserId`),
    INDEX `IX_UserSessions_SessionId` (`SessionId`),
    INDEX `IX_UserSessions_ExpiresAt` (`ExpiresAt`),
    INDEX `IX_UserSessions_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Активные сессии пользователей (персистентные, срок жизни 30 дней)';

-- =============================================
-- Таблица: UserSettings
-- Описание: Настройки пользователя (уведомления, язык, часовой пояс)
-- =============================================
CREATE TABLE IF NOT EXISTS `UserSettings` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL UNIQUE,
    `EmailNotificationsEnabled` BOOLEAN NOT NULL DEFAULT TRUE,
    `BotEventsNotifications` BOOLEAN NOT NULL DEFAULT TRUE,
    `PaymentNotifications` BOOLEAN NOT NULL DEFAULT TRUE,
    `NewsletterNotifications` BOOLEAN NOT NULL DEFAULT TRUE,
    `SecurityNotifications` BOOLEAN NOT NULL DEFAULT TRUE,
    `Language` VARCHAR(10) NOT NULL DEFAULT 'ru' COMMENT 'Язык интерфейса (ru, en)',
    `TimeZone` VARCHAR(50) NOT NULL DEFAULT 'Europe/Moscow' COMMENT 'Часовой пояс пользователя',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    CONSTRAINT `FK_UserSettings_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`)
        ON DELETE CASCADE,
    INDEX `IX_UserSettings_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Персональные настройки пользователя';

-- =============================================
-- Заполнение начальными данными (Seed Data)
-- =============================================

-- Вставка системных ролей
INSERT INTO `Roles` (`Name`, `Description`, `IsSystemRole`, `CreatedAt`) VALUES
('SuperAdmin', 'Владелец платформы с полным доступом ко всем модулям', TRUE, NOW()),
('Admin', 'Администратор платформы с правами модерации и управления', TRUE, NOW()),
('User', 'Обычный пользователь (владелец ботов)', TRUE, NOW()),
('Developer', 'Разработчик шаблонов для маркетплейса', TRUE, NOW()),
('Affiliate', 'Реферальный партнер платформы', TRUE, NOW())
ON DUPLICATE KEY UPDATE `Description` = VALUES(`Description`);

-- =============================================
-- Комментарии к схеме
-- =============================================

-- Таблицы покрывают следующие требования из ТЗ:
--
-- 1.1 Email/Password аутентификация пользователей:
--     - Users (хранение email и PasswordHash с BCrypt)
--     - LoginAttempts (логирование попыток входа)
--     - PasswordResetTokens (восстановление пароля)
--     - EmailConfirmationTokens (подтверждение email)
--     - Механизм блокировки при превышении попыток (FailedLoginAttempts, LockedOutUntil)
--
-- 1.2 Админка для управления пользователями:
--     - Users (полная информация о пользователях)
--     - Roles и UserRoles (управление ролями)
--     - LoginAttempts (просмотр истории входов)
--     - Поля для блокировки (IsBlocked, BlockedAt, BlockedReason)
--
-- 1.3 Личный кабинет пользователя:
--     - Users (редактирование профиля)
--     - UserSettings (настройки уведомлений, язык, часовой пояс)
--     - UserSessions (просмотр и завершение активных сессий)
--     - PasswordResetTokens (смена пароля)
--
-- Особенности реализации:
-- - Все пароли хешируются с использованием BCrypt (cost factor 12)
-- - Защита от брутфорса: блокировка на 15 минут после 5 неудачных попыток
-- - Токены подтверждения email действительны 24 часа
-- - Токены сброса пароля действительны 1 час
-- - Сессии хранятся в БД (персистентные) со сроком жизни 30 дней
-- - Поддержка IPv6 адресов (VARCHAR(45))
-- - UTF-8 кодировка для поддержки международных символов
-- - Каскадное удаление связанных данных при удалении пользователя
-- - Индексы на все часто используемые поля для оптимизации производительности

-- =============================================
-- Конец скрипта
-- =============================================
