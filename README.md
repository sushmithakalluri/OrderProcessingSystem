Order Processing System

Overview

A distributed microservices-based order processing system built with .NET 8, SQL Server, RabbitMQ, Redis, Docker, and CQRS principles.

The system demonstrates:

* Microservices architecture
* Event-driven communication using RabbitMQ
* Outbox Pattern for reliable messaging
* CQRS (Command Query Responsibility Segregation)
* Redis caching
* Docker containerization
* SQL Server persistence

Architecture

See the architecture diagram:

/docs/order-processing-architecture.png

Services

Order API

Responsible for:

* Creating orders
* Validating products
* Calculating order totals
* Writing Outbox events
* Publishing order events

Port:

8081

Inventory Service

Responsible for:

* Consuming order-created events
* Validating inventory availability
* Updating stock quantities
* Publishing order result events

Order Query Service

Responsible for:

* Read-only order queries
* Redis caching
* Optimized read operations

Port:

8082

Technology Stack

* .NET 8
* ASP.NET Core
* Entity Framework Core
* SQL Server
* RabbitMQ
* Redis
* Docker
* Docker Compose

Database Design

Main tables:

* Products
* Orders
* OrderItems
* OutboxMessages

Event Flow

1. Customer submits an order.
2. Order API saves order and Outbox event.
3. Outbox Publisher sends event to RabbitMQ.
4. Inventory Service consumes the event.
5. Inventory Service validates stock.
6. Inventory Service updates inventory.
7. Inventory Service publishes order result.
8. Order API updates order status.
9. Order Query Service serves read requests and caches responses in Redis.

Running the Project

Start all services:

docker compose up --build -d

Verify containers:

docker ps

API Endpoints

Create Order

POST

http://localhost:8081/api/orders

Example request:

{
  "customerId": "customer-001",
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": "22222222-2222-2222-2222-222222222222",
      "quantity": 1
    }
  ]
}

Get Order

GET

http://localhost:8082/api/orders/{orderId}

Docker Containers

* order-api
* inventory-service
* order-query-service
* order-rabbitmq
* order-redis
* order-sqlserver

Design Patterns Demonstrated

* CQRS
* Outbox Pattern
* Repository Pattern
* Dependency Injection
* Event-Driven Architecture

Future Enhancements

* Notification Service
* Authentication & Authorization
* Distributed Tracing
* Health Checks
* OpenTelemetry Monitoring

Project Status

Completed MVP demonstrating end-to-end order processing with event-driven microservices architecture.