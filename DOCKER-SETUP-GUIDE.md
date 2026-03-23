# 🐳 Microservices Docker Setup Guide

## 📋 Prerequisites

Before running the Docker containers, ensure you have installed:

- **Docker Desktop** (version 20.10+)
- **Docker Compose** (version 1.29+)

[Download Docker Desktop](https://www.docker.com/products/docker-desktop)

---

## 🚀 Quick Start - Run Everything with One Command

### From the root directory (`.Net Day 4`):

```bash
docker-compose up --build
```

This single command will:
- ✅ Build all 5 microservices
- ✅ Start all containers
- ✅ Initialize the SQL Server database
- ✅ Start RabbitMQ and Redis
- ✅ Connect all services together

---

## 📊 System Architecture

```
┌─ Client ─────────────────┐
│                           │
▼                           │
┌──────────────────────────┐
│    API Gateway (5000)    │
│  (Ocelot - Port Mapper)  │
└──────────────────────────┘
         │
    ┌────┼────┬──────────┐
    │    │    │          │
    ▼    ▼    ▼          ▼
┌────────┐ ┌────────┐ ┌────────┐ ┌──────────────┐
│  Auth  │ │ User   │ │ Order  │ │Notification │
│Service │ │Service │ │Service │ │  Service    │
│(5001)  │ │(5002)  │ │(5003)  │ │   (5004)    │
└────────┘ └────────┘ └────────┘ └──────────────┘
    │          │         │              │
    └──────────┼─────────┼──────────────┘
               │         │
            ┌──┴────┬────┴──┐
            │       │       │
            ▼       ▼       ▼
         ┌──────────────────────┐
         │   Redis (6379)       │
         │   (Cache Layer)      │
         └──────────────────────┘
            │
            │   ┌──────────────┐
            └──▶│  RabbitMQ    │
                │  (5672/15672)│
                └──────────────┘
                   │
                   ▼
            ┌──────────────────────┐
            │  SQL Server (1433)   │
            │  (Data Persistence) │
            └──────────────────────┘
```

---

## 🌐 Service Endpoints

### External Access (from localhost):

| Service | Endpoint | Purpose |
|---------|----------|---------|
| **API Gateway** | http://localhost:5000 | Main entry point |
| **Auth Service** | http://localhost:5001 | Direct access (for testing) |
| **User Service** | http://localhost:5002 | Direct access (for testing) |
| **Order Service** | http://localhost:5003 | Direct access (for testing) |
| **RabbitMQ Admin** | http://localhost:15672 | RabbitMQ Management UI |
| **SQL Server** | localhost:1433 | Database connection |
| **Redis** | localhost:6379 | Cache store |

### Internal Service Communication (within Docker network):

| Service | Internal URL |
|---------|--------------|
| Auth Service | http://authservice:80 |
| User Service | http://userservice:80 |
| Order Service | http://orderservice:80 |
| RabbitMQ | rabbitmq:5672 |
| Redis | redis:6379 |
| SQL Server | sqldb:1433 |

---

## 📝 Common Docker Compose Commands

### Start Services
```bash
# Build and start (first time)
docker-compose up --build

# Start without rebuilding
docker-compose up

# Start in background (detached mode)
docker-compose up -d --build
```

### Stop Services
```bash
# Stop without removing
docker-compose stop

# Stop and remove containers
docker-compose down

# Stop, remove containers, and volumes
docker-compose down -v
```

### View Logs
```bash
# View all service logs
docker-compose logs

# Follow logs in real-time
docker-compose logs -f

# View specific service logs
docker-compose logs -f orderservice

# View last 50 lines
docker-compose logs --tail=50
```

### Manage Containers
```bash
# List running containers
docker-compose ps

# Execute command in a container
docker-compose exec orderservice bash

# Rebuild a specific service
docker-compose build --no-cache orderservice

# Rebuild and restart a specific service
docker-compose up --build -d orderservice
```

---

## 🔑 RabbitMQ Access

### Management Console
- **URL**: http://localhost:15672
- **Username**: guest
- **Password**: guest

### Default Queue
- **Queue Name**: `order_created`
- Used for Order → Notification service communication

---

## 💾 Database Information

### SQL Server Access
- **Host**: localhost (external) / sqldb:1433 (internal)
- **Port**: 1433
- **User**: sa
- **Password**: Your_password123
- **Databases**: 
  - DotNetLearning_Auth
  - DotNetLearning_Order
  - DotNetLearningUser

### Connection from Docker Container
```
Server=sqldb;Database=DotNetLearning_Order;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

---

## ⚡ Redis Information

### Access
- **Host**: localhost:6379 (external) / redis:6379 (internal)
- **Port**: 6379
- **Authentication**: None

### Test Redis Connection
```bash
# Install redis-cli (if needed)
npm install -g redis-cli

# Connect to Redis
redis-cli -h localhost -p 6379
ping  # Should return PONG
```

---

## 🧪 Testing the System

### Test via API Gateway
```bash
# Health check
curl http://localhost:5000/

# Auth endpoints
curl http://localhost:5000/auth/login

# User endpoints
curl http://localhost:5000/user/list

# Order endpoints
curl http://localhost:5000/order/list
```

### Test RabbitMQ
1. Open http://localhost:15672
2. Login with guest/guest
3. Check Queues tab → should see `order_created` queue

### Test Redis
1. From your machine:
```bash
redis-cli -h localhost -p 6379
PING
KEYS *
```

### Test SQL Server
1. Using SQL Server Management Studio:
   - Server: localhost,1433
   - Username: sa
   - Password: Your_password123

---

## 🐛 Troubleshooting

### Container Fails to Start
```bash
# Check logs
docker-compose logs -f [service-name]

# Rebuild without cache
docker-compose build --no-cache

# Clean up and restart
docker-compose down -v
docker-compose up --build
```

### Database Connection Issues
```bash
# Verify SQL Server is running
docker-compose ps sqldb

# Check SQL Server logs
docker-compose logs sqldb

# Wait a few moments - SQL Server takes time to start
docker-compose logs -f sqldb
```

### RabbitMQ Connection Issues
```bash
# Verify RabbitMQ is running
docker-compose ps rabbitmq

# Check health
docker-compose exec rabbitmq rabbitmq-diagnostics -q ping
```

### Redis Connection Issues
```bash
# Verify Redis is running
docker-compose ps redis

# Test Redis
docker-compose exec redis redis-cli ping
```

### Port Already in Use
```bash
# Find process using port (Windows PowerShell)
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess

# Kill process or change docker-compose port mapping
```

---

## 📊 Performance Tips

1. **First Run**: Initial build takes 5-10 minutes (downloading images, building projects)
2. **Subsequent Runs**: Much faster if code hasn't changed
3. **Use `.dockerignore`**: Speeds up Docker builds by excluding unnecessary files
4. **Detached Mode**: Use `docker-compose up -d` for background running

---

## 🔐 Production Considerations

⚠️ **Current Setup is for Development Only**

For production, consider:
- ✅ Change SQL Server password
- ✅ Use environment variables for secrets
- ✅ Enable RabbitMQ authentication
- ✅ Setup Redis persistence
- ✅ Use private Docker registry
- ✅ Setup proper logging and monitoring
- ✅ Configure health checks
- ✅ Use resource limits
- ✅ Setup persistent volumes for databases

---

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [Best Practices for Writing Dockerfiles](https://docs.docker.com/develop/dev-best-practices/dockerfile_best-practices/)

---

## ✅ Verification Checklist

After running `docker-compose up --build`:

- [ ] All 8 containers started (5 services + RabbitMQ + Redis + SQL Server)
- [ ] API Gateway responds at http://localhost:5000
- [ ] Services respond at their respective ports
- [ ] RabbitMQ accessible at http://localhost:15672
- [ ] Databases created in SQL Server
- [ ] No error logs in services

---

## 🎯 Next Steps

1. ✅ Run `docker-compose up --build`
2. ✅ Verify all services are running
3. ✅ Test API endpoints
4. ✅ Monitor logs in real-time
5. ✅ Deploy with confidence!

---

**Happy Containerizing! 🚀**
