# 🚀 .NET Microservices System

A production-oriented microservices architecture built with **ASP.NET Core (.NET 8)**, implementing API Gateway, event-driven communication, distributed caching, centralized logging, and containerized deployment.

---

## 🧠 Overview

This project demonstrates a scalable backend system composed of multiple independent services communicating through well-defined patterns.

It includes:

* Authentication and authorization using JWT
* API Gateway for centralized routing
* Event-driven communication using RabbitMQ
* Distributed caching using Redis
* Centralized logging using ELK Stack
* Health checks and structured exception handling
* Docker-based deployment for all services

---

## 🏗️ Architecture

```id="arch1"
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
-------------------------------------
            ↓
        SQL Server (DB)
            ↓
-------------------------------------
| Logstash → Elasticsearch → Kibana |
-------------------------------------
```

---

## 🔧 Features

* Clean layered architecture (Controller → Service → Repository)
* Independent microservices with clear responsibilities
* Centralized routing via API Gateway (Ocelot)
* JWT-based authentication and authorization
* Event-driven messaging with RabbitMQ
* Redis cache using Cache-Aside pattern
* Global exception handling middleware
* Structured logging using Serilog
* Centralized logging pipeline (ELK Stack)
* Health checks for service availability
* Swagger for API documentation
* Fully containerized setup using Docker

---

## 🧱 Services

| Service              | Description                     | Port |
| -------------------- | ------------------------------- | ---- |
| Auth Service         | Authentication & token handling | 5001 |
| User Service         | User management                 | 5002 |
| Order Service        | Order processing                | 5003 |
| Notification Service | Event consumer                  | 5004 |
| API Gateway (Ocelot) | Entry point for all requests    | 5005 |

---

## ⚙️ Tech Stack

* ASP.NET Core (.NET 8)
* SQL Server
* Redis
* RabbitMQ
* Ocelot API Gateway
* Serilog
* Elasticsearch, Logstash, Kibana (ELK)
* Docker & Docker Compose

---

## 📊 Centralized Logging (ELK)

Logging is implemented using a centralized pipeline:

```id="elkflow"
.NET Services → Serilog → Logstash → Elasticsearch → Kibana
```

Capabilities:

* Centralized log storage
* Real-time log exploration
* Filtering by service, request, or log level
* Visualization using dashboards

Access:

* Kibana: http://localhost:5601
* Elasticsearch: http://localhost:9200

---

## 🐳 Running the System

Start all services:

```bash id="run1"
docker-compose up --build
```

---

## 🌐 Service Endpoints

| Component            | URL                    |
| -------------------- | ---------------------- |
| API Gateway          | http://localhost:5005  |
| Auth Service         | http://localhost:5001  |
| User Service         | http://localhost:5002  |
| Order Service        | http://localhost:5003  |
| Notification Service | http://localhost:5004  |
| RabbitMQ UI          | http://localhost:15672 |
| Kibana               | http://localhost:5601  |
| Redis                | localhost:6379         |
| SQL Server           | localhost:1433         |

---

## 🔐 Default Credentials

| Service    | Username | Password         |
| ---------- | -------- | ---------------- |
| RabbitMQ   | guest    | guest            |
| SQL Server | sa       | Your_password123 |

---

## 🔄 Request Flow

```id="flow1"
Client → API Gateway → Service
            ↓
        Database Operation
            ↓
     Publish Event (RabbitMQ)
            ↓
 Notification Service consumes event
```

---

## 🧪 Basic Testing

* Register and login via Auth Service
* Use JWT token for authorized endpoints
* Create and fetch data through API Gateway
* Observe caching behavior in Redis
* Verify events via Notification Service
* Explore logs in Kibana

---

## 📊 Observability

* Structured logs using Serilog
* Centralized log ingestion via Logstash
* Indexed storage in Elasticsearch
* Visualization and querying via Kibana

---

## 🔮 Enhancements

* Kubernetes-based deployment
* CI/CD pipeline integration
* Metrics and monitoring (Prometheus, Grafana)
* Alerting based on log patterns
* Performance metrics (response time tracking)

---

## 👨‍💻 Author

Arpan Rupareliya

---

## 📌 Note

This project demonstrates a modular backend system with emphasis on scalability, maintainability, and observability using modern tooling and patterns.

---
