CREATE DATABASE OrderProcessingDb;
GO

USE OrderProcessingDb;
GO

CREATE TABLE Orders (
    OrderId UNIQUEIDENTIFIER PRIMARY KEY,
    CustomerId NVARCHAR(100) NOT NULL,
    CustomerEmail NVARCHAR(255) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE TABLE Products (
    ProductId UNIQUEIDENTIFIER PRIMARY KEY,
    ProductName NVARCHAR(255) NOT NULL,
    StockQuantity INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE TABLE OrderItems (
    OrderItemId UNIQUEIDENTIFIER PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderItems_Orders
        FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId),

    CONSTRAINT FK_OrderItems_Products
        FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

CREATE TABLE OutboxMessages (
    MessageId UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(100) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ProcessedAt DATETIME2 NULL
);