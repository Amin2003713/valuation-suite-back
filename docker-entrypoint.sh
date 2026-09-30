#!/bin/bash
set -e

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &
SQL_PID=$!

# Wait for SQL Server to accept connections
echo "Waiting for SQL Server..."
for i in $(seq 1 60); do
  if (echo > /dev/tcp/127.0.0.1/1433) 2>/dev/null; then break; fi
  sleep 2
done
sleep 5   # port opens slightly before logins work

# Start the API
cd /app
dotnet Web.dll &
APP_PID=$!

# Forward shutdown signals to both processes
trap 'kill -TERM $APP_PID $SQL_PID 2>/dev/null' SIGTERM SIGINT

# Exit as soon as either process dies so the platform restarts the container
wait -n $SQL_PID $APP_PID
EXIT_CODE=$?
kill -TERM $APP_PID $SQL_PID 2>/dev/null || true
exit $EXIT_CODE