# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files in dependency order
COPY CoreLayer/*.csproj ./CoreLayer/
COPY RepositoryLayer/*.csproj ./RepositoryLayer/
COPY ServiceLayer/*.csproj ./ServiceLayer/
COPY Ironer/*.csproj ./Ironer/

# Add SQLite package to your main project
WORKDIR /src/Ironer
RUN dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# Restore dependencies for main project
RUN dotnet restore

# Copy everything else
WORKDIR /src
COPY . .

# Publish from main project
WORKDIR /src/Ironer
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create directories for database and files
RUN mkdir -p /app/data /app/wwwroot/files

# Set environment variables for standalone mode
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection=""
ENV JWT__Key=DefaultKeyForDevelopmentOnlyNotForProduction123
ENV JWT__Issuer=http://localhost:5000/
ENV JWT__Audience=IronerAPI

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Create a startup script
RUN echo '#!/bin/bash\n\
echo " Starting Ironer API..."\n\
echo " API will be available at: http://localhost:5000 (when mapped)"\n\
echo " Swagger UI: http://localhost:5000/swagger"\n\
echo " Using embedded SQLite database"\n\
echo " Ready to accept requests!"\n\
echo ""\n\
exec dotnet Ironer.dll' > /app/start.sh && chmod +x /app/start.sh

ENTRYPOINT ["/app/start.sh"]