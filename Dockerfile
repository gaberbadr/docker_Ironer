# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files in dependency order
COPY CoreLayer/*.csproj ./CoreLayer/
COPY RepositoryLayer/*.csproj ./RepositoryLayer/
COPY ServiceLayer/*.csproj ./ServiceLayer/
COPY Ironer/*.csproj ./Ironer/

# Restore dependencies
WORKDIR /src/Ironer
RUN dotnet restore

# Copy everything else
WORKDIR /src
COPY . .

# Build and publish
WORKDIR /src/Ironer
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create directories with proper permissions
RUN mkdir -p /app/wwwroot/files && \
    chmod -R 755 /app/wwwroot/files

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Ironer.dll"]