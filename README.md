# Order Processing System

A microservices-based order processing system built with .NET 8, SQL Server, RabbitMQ, Redis and Docker.

This project demonstrates backend engineering concepts commonly used in scalable e-commerce systems, including asynchronous messaging, event-driven architecture, transactional outbox, caching, and service separation.

## Current Status

In progress.

Completed so far:

- .NET 8 solution created
- Four service projects added:
  - OrderApi
  - InventoryService
  - NotificationService
  - OrderQueryService
- Docker Compose setup for RabbitMQ and Redis
- SQL Server running locally through Docker
- Initial database schema created:
  - Orders
  - OrderItems
  - Products
  - OutboxMessages

## Planned Architecture


Client
  |
  | POST /api/orders
  v
OrderApi
  |
  | Save order + outbox message
  v
SQL Server
  |
  | Outbox publisher sends event
  v
RabbitMQ
  |
  v
InventoryService
  |
  | Updates stock and order status
  v
SQL Server
  |
  | Publishes order.confirmed / order.failed
  v
RabbitMQ
  |
  v
NotificationService

Client
  |
  | GET /api/orders/{id}
  v
OrderQueryService
  |
  | Check cache
  v
Redis
  |
  | Cache miss
  v
SQL Server
