#!/bin/bash

PROJECT_FILE="/var/www/maltalist/api/MaltalistApi.csproj"
LOG_FILE="/var/log/maltalist-api.log"

log_message() {
    echo "$(date '+%Y-%m-%d %H:%M:%S'): $1" >> "$LOG_FILE"
}

# Find the PID of the running dotnet process for the project
PID=$(pgrep -f "dotnet run.*maltalist/api/MaltalistApi.csproj")

if [ -z "$PID" ]; then
    log_message "No running dotnet process found for $PROJECT_FILE."
    echo "No running process found."
    exit 0
fi

log_message "Attempting graceful shutdown of dotnet process PID $PID for $PROJECT_FILE."
kill "$PID"

# Wait up to 15 seconds for graceful shutdown
for i in {1..15}; do
    if ! kill -0 "$PID" 2>/dev/null; then
        log_message "Process $PID stopped gracefully."
        echo "Stopped gracefully."
        exit 0
    fi
    sleep 1
done

log_message "Process $PID did not stop gracefully, sending SIGKILL."
kill -9 "$PID"
echo "Force killed process $PID."