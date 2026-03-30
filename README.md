# 🚀 .NET Microservices System

A production-oriented microservices architecture built with **ASP.NET Core (.NET 8)**, implementing API Gateway, event-driven communication, distributed caching, centralized logging, distributed tracing, and containerized deployment.

---

## 🧠 Overview

This project demonstrates a scalable backend system composed of multiple independent services communicating through well-defined patterns.

It includes:

* Authentication and authorization using JWT
* API Gateway for centralized routing
* Event-driven communication using RabbitMQ + MassTransit
* Distributed caching using Redis
* Centralized logging using ELK Stack (Serilog → Logstash → Elasticsearch → Kibana)
* Distributed tracing and metrics using OpenTelemetry + SigNoz
* End-to-end trace propagation across HTTP and RabbitMQ message hops
* Health checks and structured exception handling
* Docker-based deployment for all services

---

## 🏗️ Architecture

```
Client
   ↓
API Gateway (Ocelot - 5005)
   ↓
-------------------------------------
|   Auth Service   |   User Service  |
-------------------------------------
            ↓
        Order Service
            ↓
-------------------------------------
| Redis (Cache) | RabbitMQ (Events) |
|               |   via MassTransit |
-------------------------------------
            ↓
        SQL Server (DB)
            ↓
-------------------------------------
| Logstash → Elasticsearch → Kibana |  ← Logs
-------------------------------------
            ↓
-------------------------------------
| OpenTelemetry → SigNoz Collector  |  ← Traces & Metrics
-------------------------------------
```

---

## 🔧 Features

* Clean layered architecture (Controller → Service → Repository)
* Independent microservices with clear responsibilities
* Centralized routing via API Gateway (Ocelot)
* JWT-based authentication and authorization
* Event-driven messaging with RabbitMQ via MassTransit
* End-to-end distributed tracing across HTTP and message bus hops
* ASP.NET Core, HttpClient, runtime, and process metrics
* Redis cache using Cache-Aside pattern
* Global exception handling middleware
* Structured logging using Serilog → ELK Stack
* Distributed tracing and metrics via OpenTelemetry → SigNoz
* Health checks for service availability
* Swagger for API documentation
* Fully containerized setup using Docker

---

## 🧱 Services

| Service              | Description                     | Port |
| -------------------- | ------------------------------- | ---- |
| Auth Service         | Authentication & token handling | 5001 |
| User Service         | User management                 | 5002 |
| Order Service        | Order processing & event publish | 5003 |
| Notification Service | MassTransit event consumer      | 5004 |
| API Gateway (Ocelot) | Entry point for all requests    | 5005 |

---

## ⚙️ Tech Stack

* ASP.NET Core (.NET 8)
* SQL Server
* Redis
* RabbitMQ + MassTransit
* Ocelot API Gateway
* Serilog
* Elasticsearch, Logstash, Kibana (ELK)
* OpenTelemetry (OTLP/gRPC)
* SigNoz (distributed tracing & metrics)
* Docker & Docker Compose

---

## 📊 Centralized Logging (ELK)

Logging is implemented using a centralized pipeline:

```
.NET Services → Serilog → Logstash → Elasticsearch → Kibana
```

Capabilities:

* Centralized log storage across all services
* Real-time log exploration
* Filtering by service, request, or log level
* Visualization using dashboards

Access:

* Kibana: http://localhost:5601
* Elasticsearch: http://localhost:9200

---

## 🔭 Distributed Tracing & Metrics (SigNoz)

All services export traces and metrics to SigNoz via OpenTelemetry Protocol (OTLP over gRPC).

```
.NET Services → OpenTelemetry SDK → SigNoz OTLP Collector (4317) → SigNoz UI (8080)
```

What is captured:

* Full end-to-end traces: API Gateway → Service → RabbitMQ publish → Consumer
* ASP.NET Core request spans with exception recording
* HttpClient outbound call spans
* MassTransit publish and consume spans (trace propagation across RabbitMQ)
* Runtime metrics (GC, thread pool, memory)
* Process metrics (working set, CPU time, handle count)

Access:

* SigNoz UI: http://localhost:8080

Start SigNoz (runs in `signoz/` inside this project folder):

```bash
cd signoz/deploy/docker
docker compose up -d
```

---

## 🐳 Running the System

Start SigNoz first (one-time setup):

```bash
cd signoz/deploy/docker
docker compose up -d
cd ../../..
```

Then start all microservices:

```bash
docker-compose up --build
```

---

## 🌐 Service Endpoints

| Component             | URL                    |
| --------------------- | ---------------------- |
| API Gateway           | http://localhost:5005  |
| Auth Service          | http://localhost:5001  |
| User Service          | http://localhost:5002  |
| Order Service         | http://localhost:5003  |
| Notification Service  | http://localhost:5004  |
| RabbitMQ UI           | http://localhost:15672 |
| Kibana (Logs)         | http://localhost:5601  |
| SigNoz (Traces)       | http://localhost:8080  |
| Redis                 | localhost:6379         |
| SQL Server            | localhost:1433         |
| SigNoz OTLP Collector | localhost:4317         |

---

## 🔐 Default Credentials

| Service       | Username      | Password         |
| ------------- | ------------- | ---------------- |
| RabbitMQ      | guest         | guest            |
| SQL Server    | sa            | Your_password123 |
| Elasticsearch | elastic       | elastic123       |
| Kibana        | kibana_system | kibana_system123 |

---

## 🔄 Request Flow

```
Client → API Gateway → Service
            ↓
        Database Operation
            ↓
  MassTransit Publish (RabbitMQ)   ← trace context propagated here
            ↓
 Notification Service consumes event
            ↓
  Full trace visible in SigNoz UI
```

---

## 🧪 Basic Testing

* Register and login via Auth Service
* Use JWT token for authorized endpoints
* Create and fetch data through API Gateway
* Create an order → observe the MassTransit event in Notification Service logs
* Observe caching behavior in Redis
* Explore logs in Kibana
* Open SigNoz at http://localhost:8080 → Services to see all five services
* Open SigNoz → Traces to see the full end-to-end trace for an order creation

---

## 📊 Observability

| Concern         | Tool                          | URL                    |
| --------------- | ----------------------------- | ---------------------- |
| Logs            | Serilog + ELK Stack           | http://localhost:5601  |
| Traces          | OpenTelemetry + SigNoz        | http://localhost:8080  |
| Metrics         | OpenTelemetry + SigNoz        | http://localhost:8080  |
| Message tracing | MassTransit + OpenTelemetry   | Visible in SigNoz      |

---

## 🔮 Enhancements

* Kubernetes-based deployment
* CI/CD pipeline integration
* Alerting based on SigNoz trace/metric thresholds
* Custom business metrics using OpenTelemetry Meter API

---

## 👨‍💻 Author

Arpan Rupareliya

---

## 📌 Note

This project demonstrates a modular backend system with emphasis on scalability, maintainability, and full-stack observability using modern tooling and patterns.

---
