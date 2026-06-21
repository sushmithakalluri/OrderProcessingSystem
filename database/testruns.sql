USE OrderProcessingDb;
GO

SELECT TOP 5
    OrderId,
    CustomerId,
    TotalAmount,
    Status,
    CreatedAt,
    UpdatedAt
FROM Orders
ORDER BY CreatedAt DESC;