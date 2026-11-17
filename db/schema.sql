IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'Pr4_Gordov')
BEGIN
    CREATE DATABASE [Pr4_Gordov];
END
GO

USE [Pr4_Gordov];
GO

EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";
GO

DROP TABLE IF EXISTS [Reviews];
DROP TABLE IF EXISTS [Favorites];
DROP TABLE IF EXISTS [CartItems];
DROP TABLE IF EXISTS [OrderItems];
DROP TABLE IF EXISTS [Orders];
DROP TABLE IF EXISTS [Products];
DROP TABLE IF EXISTS [PickupPoints];
DROP TABLE IF EXISTS [Categories];
DROP TABLE IF EXISTS [Users];
DROP TABLE IF EXISTS [DeliveryTypes];
DROP TABLE IF EXISTS [OrderStatuses];
DROP TABLE IF EXISTS [Roles];
GO

CREATE TABLE [Roles] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE [OrderStatuses] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE [DeliveryTypes] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE [Categories] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE [PickupPoints] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(150) NOT NULL,
    [Address] NVARCHAR(255) NOT NULL,
    [WorkingHours] NVARCHAR(100) NOT NULL DEFAULT '09:00 - 21:00'
);
GO

CREATE TABLE [Users] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Login] NVARCHAR(100) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [RoleId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [Roles]([Id])
);
GO

CREATE TABLE [Products] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Price] DECIMAL(10,2) NOT NULL CHECK ([Price] >= 0),
    [CategoryId] INT NOT NULL,
    [ImagePath] NVARCHAR(255) NULL,
    [PackageQuantity] INT NULL CHECK ([PackageQuantity] IS NULL OR [PackageQuantity] > 0),
    [StockQuantity] INT NOT NULL DEFAULT 0 CHECK ([StockQuantity] >= 0),
    [IsAvailable] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([Id])
);
GO

CREATE TABLE [Orders] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [OrderDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [StatusId] INT NOT NULL,
    [DeliveryTypeId] INT NOT NULL,
    [DeliveryAddress] NVARCHAR(255) NULL,
    [PickupPointId] INT NULL,
    [TotalCost] DECIMAL(10,2) NOT NULL CHECK ([TotalCost] >= 0),
    CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_Orders_OrderStatuses] FOREIGN KEY ([StatusId]) REFERENCES [OrderStatuses]([Id]),
    CONSTRAINT [FK_Orders_DeliveryTypes] FOREIGN KEY ([DeliveryTypeId]) REFERENCES [DeliveryTypes]([Id]),
    CONSTRAINT [FK_Orders_PickupPoints] FOREIGN KEY ([PickupPointId]) REFERENCES [PickupPoints]([Id])
);
GO

CREATE TABLE [OrderItems] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL CHECK ([Quantity] > 0),
    [UnitPrice] DECIMAL(10,2) NOT NULL CHECK ([UnitPrice] >= 0),
    CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id])
);
GO

CREATE TABLE [CartItems] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL DEFAULT 1 CHECK ([Quantity] > 0),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_CartItems_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]),
    CONSTRAINT [UQ_CartItems_User_Product] UNIQUE ([UserId], [ProductId])
);
GO

CREATE TABLE [Favorites] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Favorites_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Favorites_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]),
    CONSTRAINT [UQ_Favorites_User_Product] UNIQUE ([UserId], [ProductId])
);
GO

CREATE TABLE [Reviews] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ProductId] INT NOT NULL,
    [UserId] INT NOT NULL,
    [Rating] INT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
    [Comment] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [UQ_Reviews_User_Product] UNIQUE ([UserId], [ProductId])
);
GO

CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [Products]([CategoryId]);
CREATE NONCLUSTERED INDEX [IX_Products_Price] ON [Products]([Price]);
CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [Orders]([UserId]);
CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [OrderItems]([OrderId]);
CREATE NONCLUSTERED INDEX [IX_CartItems_UserId] ON [CartItems]([UserId]);
CREATE NONCLUSTERED INDEX [IX_Favorites_UserId] ON [Favorites]([UserId]);
CREATE NONCLUSTERED INDEX [IX_Reviews_ProductId] ON [Reviews]([ProductId]);
GO

INSERT INTO [Roles] ([Name]) VALUES 
(N'Admin'),
(N'User');
GO

INSERT INTO [OrderStatuses] ([Name]) VALUES 
(N'Оформлен'),
(N'В обработке'),
(N'Передан в доставку'),
(N'Готов к выдаче'),
(N'Получен'),
(N'Отменён');
GO

INSERT INTO [DeliveryTypes] ([Name]) VALUES 
(N'ПВЗ (Самовывоз)'),
(N'Доставка по адресу');
GO

INSERT INTO [Categories] ([Name]) VALUES 
(N'Футболки и майки'),
(N'Худи и толстовки'),
(N'Брюки и штаны'),
(N'Аксессуары и мерч');
GO

INSERT INTO [PickupPoints] ([Name], [Address], [WorkingHours]) VALUES 
(N'ПВЗ №1 Академический', N'г. Москва, ул. Вавилова, д. 7', N'09:00 - 21:00'),
(N'ПВЗ №2 Университетский', N'г. Москва, пр. Вернадского, д. 33', N'10:00 - 22:00'),
(N'ПВЗ №3 Бауманский', N'г. Москва, ул. Малая Почтовая, д. 19', N'09:00 - 21:00'),
(N'ПВЗ №4 Фили', N'г. Москва, Багратионовский пр-д, д. 71', N'10:00 - 20:00');
GO

INSERT INTO [Users] ([Login], [PasswordHash], [RoleId]) VALUES 
(N'admin', N'AQAAAAIAAYagAAAAEP2E7R2a1dC5p326K2aC4P1K0JzW0wQYvRzKj1L1v2m3N4o5P6q7R8s9T0u1V2w3==', 1),
(N'korzh', N'AQAAAAIAAYagAAAAEK6h7P8x9q1r2s3t4u5v6w7x8y9z0a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r==', 2),
(N'nika',  N'AQAAAAIAAYagAAAAEN7i8Q9y0r1s2t3u4v5w6x7y8z9a0b1c2d3e4f5g6h7i8j9k0l1m2n3o4p5q6r7s==', 2);
GO

INSERT INTO [Products] ([Title], [Description], [Price], [CategoryId], [ImagePath], [PackageQuantity], [StockQuantity], [IsAvailable]) VALUES 
(N'Оверсайз футболка GMWOG Classic Cyber', N'Плотная хлопковая футболка с авторским принтом GMWOG.', 2490.00, 1, N'tshirt_cyber.png', NULL, 30, 1),
(N'Базовая белая футболка Minimal Core', N'100% гребенной хлопок. Идеально под худи или пиджак.', 1790.00, 1, N'tshirt_white.png', NULL, 45, 1),
(N'Худи Charcoal Oversize с начесом', N'Премиальный футер 3-х нитка с мягким внутренним начесом.', 4890.00, 2, N'hoodie_dark.png', NULL, 20, 1),
(N'Зип-худи GMWOG Acid Wash', N'Винтажная варка ткани, усиленные молнии YKK.', 5490.00, 2, N'hoodie_zip.png', NULL, 15, 1),
(N'Карго-брюки Street Tactical', N'Удобные хлопковые брюки с глубокими боковыми карманами.', 3990.00, 3, N'pants_cargo.png', NULL, 25, 1),
(N'Носки GMWOG High Crew (3 пары в уп.)', N'Плотные высокие носки с вышитым логотипом.', 490.00, 4, N'socks_pack.png', 3, 50, 1),
(N'Стикерпак инвестора (10 наклеек)', N'Виниловые влагостойкие наклейки для ноутбука и телефона.', 290.00, 4, N'stickers_investor.png', 10, 100, 1),
(N'Шоппер холщовый «За мной едет акционер»', N'Плотная сумка-шоппер с ироничным принтом.', 890.00, 4, N'shopper_nagiev.png', NULL, 40, 1),
(N'Лимитированная кепка GMWOG Black Edition', N'Черная хлопковая кепка с регулируемым ремешком.', 1490.00, 4, N'cap_black.png', NULL, 0, 0);
GO

INSERT INTO [Reviews] ([ProductId], [UserId], [Rating], [Comment]) VALUES 
(1, 2, 5, N'good'),
(1, 3, 3, N'норм'),
(2, 2, 1, N'говно'),
(3, 3, 5, N'топ'),
(6, 2, 4, N'пойдет');
GO

INSERT INTO [Orders] ([UserId], [StatusId], [DeliveryTypeId], [DeliveryAddress], [PickupPointId], [TotalCost]) VALUES 
(2, 4, 1, NULL, 1, 2490.00);
INSERT INTO [OrderItems] ([OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES 
(1, 1, 1, 2490.00);

INSERT INTO [Orders] ([UserId], [StatusId], [DeliveryTypeId], [DeliveryAddress], [PickupPointId], [TotalCost]) VALUES 
(3, 1, 2, N'г. Москва, пр-т Вернадского, д. 78, кв. 112', NULL, 5380.00);
INSERT INTO [OrderItems] ([OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES 
(2, 3, 1, 4890.00),
(2, 6, 1, 490.00);

INSERT INTO [CartItems] ([UserId], [ProductId], [Quantity]) VALUES 
(2, 2, 1),
(3, 9, 2);

INSERT INTO [Favorites] ([UserId], [ProductId]) VALUES 
(2, 3),
(3, 1);
GO
