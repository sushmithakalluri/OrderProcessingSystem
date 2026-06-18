USE OrderProcessingDb;
GO

SELECT TOP 5
    MessageId,
    EventType,
    Status,
    CreatedAt,
    ProcessedAt,
    Payload
FROM OutboxMessages
ORDER BY CreatedAt DESC;