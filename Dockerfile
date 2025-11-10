# TaxFlow Enterprise - Multi-stage Dockerfile for API

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TaxFlow.Enterprise.sln", "./"]
COPY ["src/TaxFlow.Core/TaxFlow.Core.csproj", "src/TaxFlow.Core/"]
COPY ["src/TaxFlow.Infrastructure/TaxFlow.Infrastructure.csproj", "src/TaxFlow.Infrastructure/"]
COPY ["src/TaxFlow.Application/TaxFlow.Application.csproj", "src/TaxFlow.Application/"]
COPY ["src/TaxFlow.Api/TaxFlow.Api.csproj", "src/TaxFlow.Api/"]

# Restore dependencies
RUN dotnet restore "src/TaxFlow.Api/TaxFlow.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/TaxFlow.Api"
RUN dotnet build "TaxFlow.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "TaxFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install necessary runtime dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -u 1000 taxflow && \
    chown -R taxflow:taxflow /app

# Copy published app
COPY --from=publish /app/publish .

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs && \
    chown -R taxflow:taxflow /app/data /app/logs

# Switch to non-root user
USER taxflow

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "TaxFlow.Api.dll"]
