#!/bin/bash

# Path to the .NET project and log file
PROJECT_FILE="/var/www/maltalist/api/MaltalistApi.csproj"
LOG_FILE="/var/log/maltalist-api.log"
PORT="5023"

# Log function to append messages with timestamp
log_message() {
    echo "$(date '+%Y-%m-%d %H:%M:%S'): $1" >> "$LOG_FILE"
}

# Check if dotnet run is already running for the project
if pgrep -f "dotnet run.*maltalist/api/MaltalistApi.csproj" > /dev/null; then
    log_message "Dotnet run is already running for $PROJECT_FILE."
    echo "Running"
    exit 0
else
    log_message "Dotnet run is not running for $PROJECT_FILE."
    echo "Not running"
fi

# Check if port is in use
if ss -tuln | grep -q ":$PORT"; then
    log_message "Port $PORT is in use. Attempting to terminate process."
    # Find and kill the process using the port
    PORT_PID=$(ss -tulnp | grep ":$PORT" | grep -oP 'pid=\K[0-9]+' | head -1)
    if [ -n "$PORT_PID" ]; then
        log_message "Killing process PID $PORT_PID using port $PORT."
        kill -9 "$PORT_PID" 2>/dev/null
        sleep 1
        # Verify port is free
        if ss -tuln | grep -q ":$PORT"; then
            log_message "Failed to free port $PORT."
            echo "Error: Failed to free port $PORT"
            exit 1
        else
            log_message "Port $PORT successfully freed."
        fi
    else
        log_message "No PID found for port $PORT, but port is in use. Attempting to free."
        fuser -k "$PORT/tcp" 2>/dev/null
        sleep 1
        if ss -tuln | grep -q ":$PORT"; then
            log_message "Failed to free port $PORT using fuser."
            echo "Error: Failed to free port $PORT"
            exit 1
        else
            log_message "Port $PORT successfully freed using fuser."
        fi
    fi
else
    log_message "Port $PORT is free."
fi

# Verify project file exists
if [ ! -f "$PROJECT_FILE" ]; then
    log_message "Error: Project file $PROJECT_FILE not found."
    echo "Error: Project file not found"
    exit 1
fi

log_message "Starting dotnet run for $PROJECT_FILE..."

# Run dotnet in the background, appending output to log file
nohup dotnet run --project "$PROJECT_FILE" >> "$LOG_FILE" 2>&1 &

NEW_PID=$!
log_message "Dotnet run started with PID $NEW_PID"
echo "Started with PID $NEW_PID"