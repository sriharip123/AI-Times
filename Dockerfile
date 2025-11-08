# Multi-stage Dockerfile for JSON-Whisperer

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["JSON-Whisperer/JSON-Whisperer.csproj", "JSON-Whisperer/"]
COPY ["JSON-Whisperer.Tests/JSON-Whisperer.Tests.csproj", "JSON-Whisperer.Tests/"]

# Restore dependencies
RUN dotnet restore "JSON-Whisperer/JSON-Whisperer.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/JSON-Whisperer"
RUN dotnet build "JSON-Whisperer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "JSON-Whisperer.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create necessary directories
RUN mkdir -p /app/logs /app/AppData/examples && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=publish /app/publish .

# Copy AppData directory if it exists
COPY --chown=appuser:appuser AppData/ ./AppData/ 2>/dev/null || true

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD dotnet JSON-Whisperer.dll --health-check || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "JSON-Whisperer.dll"]