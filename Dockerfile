# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files in dependency order
COPY CoreLayer/*.csproj ./CoreLayer/
COPY RepositoryLayer/*.csproj ./RepositoryLayer/
COPY ServiceLayer/*.csproj ./ServiceLayer/
COPY Ironer/*.csproj ./Ironer/

# Add SQLite package
WORKDIR /src/Ironer
RUN dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# Restore dependencies
RUN dotnet restore

# Copy everything else
WORKDIR /src
COPY . .

# Build the project
WORKDIR /src/Ironer
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create directories with proper permissions
RUN mkdir -p /app/data /app/wwwroot/files && \
    chmod -R 755 /app/data /app/wwwroot/files

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/ironer.db"

# Expose port
EXPOSE 8080

# Create startup script that ensures database is properly initialized
RUN echo '#!/bin/bash' > /app/start.sh && \
    echo 'set -e' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo 'echo " Starting Ironer API..."' >> /app/start.sh && \
    echo 'echo " Initializing SQLite database..."' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Ensure data directory exists and has proper permissions' >> /app/start.sh && \
    echo 'mkdir -p /app/data' >> /app/start.sh && \
    echo 'chmod 755 /app/data' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Set the connection string to use our data directory' >> /app/start.sh && \
    echo 'export ConnectionStrings__DefaultConnection="Data Source=/app/data/ironer.db"' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo 'echo " Starting application with automatic migrations..."' >> /app/start.sh && \
    echo 'echo " Database will be created at: /app/data/ironer.db"' >> /app/start.sh && \
    echo 'echo " API will be available at: http://localhost:5000 (when mapped)"' >> /app/start.sh && \
    echo 'echo " Swagger UI: http://localhost:5000/swagger"' >> /app/start.sh && \
    echo 'echo " Ready to accept requests!"' >> /app/start.sh && \
    echo 'echo ""' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start the application - migrations will run automatically in Program.cs' >> /app/start.sh && \
    echo 'exec dotnet Ironer.dll' >> /app/start.sh && \
    chmod +x /app/start.sh

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/start.sh"]