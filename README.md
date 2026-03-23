# 🚀 .NET Microservices System (Production-Ready)

A **production-grade microservices architecture** built using **ASP.NET Core**, implementing modern backend patterns like **API Gateway, Event-Driven Architecture, Caching, and Containerization**.

---

## 🧠 Overview

This project demonstrates how to design and build a **scalable, distributed backend system** using industry-standard tools and practices.

It includes:

* 🔐 Authentication & Authorization (JWT)
* 🧱 Microservices Architecture
* 🚪 API Gateway (Ocelot)
* 📬 Event-driven communication (RabbitMQ)
* ⚡ Distributed caching (Redis)
* 🐳 Dockerized deployment
* 📊 Logging, Health Checks & Exception Handling

---

## 🏗️ Architecture

```
Client
   ↓
API Gateway (Ocelot)
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
```

---

## 🔥 Features

* ✅ Clean Architecture (Controller → Service → Repository)
* ✅ Microservices-based system design
* ✅ API Gateway with centralized routing
* ✅ JWT-based authentication
* ✅ Event-driven architecture using RabbitMQ
* ✅ Redis caching (Cache-Aside Pattern)
* ✅ Global exception handling
* ✅ Structured logging using Serilog
* ✅ Health checks for monitoring
* ✅ Swagger API documentation
* ✅ Dockerized multi-service deployment

---

## 🧱 Microservices

| Service                 | Description                             | Port |
| ----------------------- | --------------------------------------- | ---- |
| 🔐 Auth Service         | Handles authentication (Login/Register) | 5001 |
| 👤 User Service         | Manages user data                       | 5002 |
| 📦 Order Service        | Handles order operations                | 5003 |
| 📩 Notification Service | Consumes events (RabbitMQ)              | 5004 |
| 🚪 API Gateway          | Single entry point (Ocelot)             | 5000 |

---

## ⚙️ Tech Stack

* **Backend:** ASP.NET Core (.NET 8)
* **Database:** SQL Server
* **Cache:** Redis
* **Messaging:** RabbitMQ
* **API Gateway:** Ocelot
* **Containerization:** Docker & Docker Compose
* **Logging:** Serilog

---

## 🐳 Run the Project (Docker)

> 🚀 Start entire system with one command

```bash
docker-compose up --build
```

---

## 🌐 Access Services

| Component               | URL                    |
| ----------------------- | ---------------------- |
| 🚪 API Gateway          | http://localhost:5000  |
| 🔐 Auth Service         | http://localhost:5001  |
| 👤 User Service         | http://localhost:5002  |
| 📦 Order Service        | http://localhost:5003  |
| 📩 Notification Service | http://localhost:5004  |
| 🐇 RabbitMQ UI          | http://localhost:15672 |
| 🗄️ SQL Server          | localhost:1433         |
| ⚡ Redis                 | localhost:6379         |

---

## 🔐 Default Credentials

| Service    | Username | Password         |
| ---------- | -------- | ---------------- |
| RabbitMQ   | guest    | guest            |
| SQL Server | sa       | Your_password123 |

---

## 📊 Key Concepts Implemented

* 🧩 Microservices Architecture
* 🔄 API Gateway Pattern
* 📬 Event-Driven Communication
* ⚡ Caching (Redis - Cache Aside Pattern)
* 🐳 Containerization (Docker)
* 🔐 Authentication (JWT)
* 🧠 Distributed System Design

---

## 🔄 Request Flow Example

```
Client → API Gateway → Order Service
            ↓
        Save to DB
            ↓
     Publish Event (RabbitMQ)
            ↓
 Notification Service consumes event
            ↓
     Send Email / Log
```

---

## 🧪 Testing the System

1. Register a user → `/auth/register`
2. Login → `/auth/login`
3. Use JWT token
4. Create order → `/order`
5. Check Redis caching (2nd call faster ⚡)
6. Verify RabbitMQ event in Notification Service logs

---

## 📸 Screenshots (Optional)

> Add screenshots here for better presentation

```
docs/images/swagger.png
docs/images/rabbitmq.png
docs/images/docker.png
```

---

## 🧠 What I Learned

* Designing scalable backend systems
* Implementing microservices communication
* Using message brokers (RabbitMQ)
* Applying caching strategies (Redis)
* Containerizing applications with Docker
* Production-ready API design

---

## 🚀 Future Enhancements

* Kubernetes deployment
* CI/CD pipeline (GitHub Actions)
* Centralized logging (ELK Stack)
* Monitoring (Prometheus + Grafana)
* Rate limiting & security improvements

---

## 👨‍💻 Author

**Arpan Rupareliya**

---

## ⭐ If you like this project

Give it a ⭐ on GitHub and feel free to fork or contribute!

---

## 💥 Final Note

> This project represents a **real-world backend system design** using modern technologies and best practices, suitable for production-level applications.
