@echo off
REM Docker Microservices Startup Script
REM This script builds and runs the entire microservices system with Docker

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     🐳 .NET Microservices Docker Startup Script 🐳       ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM Check if Docker is installed
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ ERROR: Docker is not installed or not in PATH
    echo.
    echo Please install Docker Desktop from: https://www.docker.com/products/docker-desktop
    echo.
    pause
    exit /b 1
)

echo ✅ Docker found: 
docker --version
echo.

REM Check if Docker Daemon is running
docker ps >nul 2>&1
if errorlevel 1 (
    echo ❌ ERROR: Docker daemon is not running
    echo.
    echo Please start Docker Desktop and try again
    echo.
    pause
    exit /b 1
)

echo ✅ Docker daemon is running
echo.

REM Display system info
echo 📊 Starting microservices...
echo.
echo Services to be started:
echo   ✓ Auth Service        (port 5001, internal: 80)
echo   ✓ User Service        (port 5002, internal: 80)
echo   ✓ Order Service       (port 5003, internal: 80)
echo   ✓ Notification Service (port 5004, internal: 80)
echo   ✓ API Gateway         (port 5000, internal: 80)
echo   ✓ RabbitMQ            (port 5672, admin: 15672)
echo   ✓ Redis               (port 6379)
echo   ✓ SQL Server          (port 1433)
echo.

REM Run docker-compose
echo 🚀 Building and starting containers...
echo    (First run will take 5-10 minutes, subsequent runs will be faster)
echo.

docker-compose up --build

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║         ✅ System startup complete! ✅                    ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo 🌐 Access your services at:
echo    API Gateway:     http://localhost:5000
echo    RabbitMQ Admin:  http://localhost:15672 (guest/guest)
echo    Auth Service:    http://localhost:5001
echo    User Service:    http://localhost:5002
echo    Order Service:   http://localhost:5003
echo    Notification:    http://localhost:5004
echo.
echo 💾 Database:
echo    Server:   localhost,1433
echo    User:     sa
echo    Password: Your_password123
echo.
echo 📊 For more information, see:
echo    - DOCKER-SETUP-GUIDE.md (comprehensive guide)
echo    - DOCKER-IMPLEMENTATION-SUMMARY.md (quick reference)
echo.
echo Press Ctrl+C to stop the system.
echo.
