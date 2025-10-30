# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SendGridClickTrackTLSProxy.sln ./
COPY src/SendGridClickTrackTLSProxy/SendGridClickTrackTLSProxy.csproj src/SendGridClickTrackTLSProxy/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/SendGridClickTrackTLSProxy/ src/SendGridClickTrackTLSProxy/

# Build the application
WORKDIR /src/src/SendGridClickTrackTLSProxy
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install ca-certificates for HTTPS connections
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Railway provides PORT dynamically, so we use a script to handle it
COPY <<EOF /app/docker-entrypoint.sh
#!/bin/sh
export ASPNETCORE_URLS="http://+:\${PORT:-80}"
exec dotnet SendGridClickTrackTLSProxy.dll
EOF

RUN chmod +x /app/docker-entrypoint.sh

# Expose default port (Railway will override with PORT env var)
EXPOSE 80

# Start the application
ENTRYPOINT ["/app/docker-entrypoint.sh"]
