# -------------------- BASE IMAGE --------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# -------------------- BUILD STAGE --------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file (FIXED PATH)
COPY ["NotificationService.csproj", "./"]

RUN dotnet restore "NotificationService.csproj"

# Copy remaining files
COPY . .

RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

# -------------------- PUBLISH STAGE --------------------
FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------------------- FINAL STAGE --------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NotificationService.dll"]