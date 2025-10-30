#!/bin/bash

# Exit on any error
set -e

echo "Starting SendGrid Click Track TLS Proxy deployment..."

# Install .NET SDK if not present
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET SDK..."
    # Download and install .NET 9.0 SDK
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --version 9.0.100
    export PATH="$PATH:$HOME/.dotnet"
fi

# Display .NET version
echo "Using .NET version:"
dotnet --version

# Change to the project directory
cd src/SendGridClickTrackTLSProxy

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build the application
echo "Building application..."
dotnet build -c Release

# Set environment variables for production
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-80}"

# Run the application
echo "Starting application on port ${PORT:-80}..."
exec dotnet run -c Release --no-build
