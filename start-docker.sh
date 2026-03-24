#!/bin/bash
# Docker Microservices Startup Script (Linux/macOS)
# This script builds and runs the entire microservices system with Docker

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║     🐳 .NET Microservices Docker Startup Script 🐳       ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ ERROR: Docker is not installed"
    echo ""
    echo "Please install Docker from: https://www.docker.com/products/docker-desktop"
    echo ""
    exit 1
fi

echo "✅ Docker found:"
docker --version
echo ""

# Check if Docker daemon is running
if ! docker ps &> /dev/null; then
    echo "❌ ERROR: Docker daemon is not running"
    echo ""
    echo "Please start Docker and try again"
    echo ""
    exit 1
fi

echo "✅ Docker daemon is running"
echo ""

# Display system info
echo "📊 Starting microservices..."
echo ""
echo "Services to be started:"
echo "  ✓ Auth Service        (port 5001, internal: 80)"
echo "  ✓ User Service        (port 5002, internal: 80)"
echo "  ✓ Order Service       (port 5003, internal: 80)"
echo "  ✓ Notification Service (port 5004, internal: 80)"
echo "  ✓ API Gateway         (port 5005, internal: 80)"
echo "  ✓ RabbitMQ            (port 5672, admin: 15672)"
echo "  ✓ Redis               (port 6379)"
echo "  ✓ SQL Server          (port 1433)"
echo ""

# Run docker-compose
echo "🚀 Building and starting containers..."
echo "   (First run will take 5-10 minutes, subsequent runs will be faster)"
echo ""

docker-compose up --build

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║         ✅ System startup complete! ✅                    ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "🌐 Access your services at:"
echo "   API Gateway:     http://localhost:5005"
echo "   RabbitMQ Admin:  http://localhost:15672 (guest/guest)"
echo "   Auth Service:    http://localhost:5001"
echo "   User Service:    http://localhost:5002"
echo "   Order Service:   http://localhost:5003"
echo "   Notification:    http://localhost:5004"
echo ""
echo "💾 Database:"
echo "   Server:   localhost,1433"
echo "   User:     sa"
echo "   Password: Your_password123"
echo ""
echo "📊 For more information, see:"
echo "   - DOCKER-SETUP-GUIDE.md (comprehensive guide)"
echo "   - DOCKER-IMPLEMENTATION-SUMMARY.md (quick reference)"
echo ""
echo "Press Ctrl+C to stop the system."
echo ""
