#!/bin/bash

# This script builds and runs the application using Docker
# Railway should detect Docker and use it automatically

echo "Building SendGrid Click Track TLS Proxy with Docker..."

# Build the Docker image
docker build -t sendgrid-proxy .

# Run the container with the PORT environment variable
exec docker run -p ${PORT:-80}:80 -e PORT=${PORT:-80} sendgrid-proxy
