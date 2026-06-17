USE OrderProcessingDb;

GO

SELECT TOP 5 *

FROM OutboxMessages

ORDER BY CreatedAt DESC;