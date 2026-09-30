#!/bin/bash
set -e

# 1. Start SQL Server in the background
/opt/mssql/bin/sqlservr &
SQL_PID=$!

# 2. Wait until SQL Server is really ready for logins (max ~3 min)
echo "Waiting for SQL Server to be ready..."
READY=0
for i in $(seq 1 90); do
  if ! kill -0 $SQL_PID 2>/dev/null; then
    echo "SQL Server exited before becoming ready" >&2
    exit 1
  fi
  if grep -q "SQL Server is now ready for client connections" /var/opt/mssql/log/errorlog 2>/dev/null; then
    READY=1
    break
  fi
  sleep 2
done

if [ "$READY" -ne 1 ]; then
  echo "Timed out waiting for SQL Server" >&2
  kill -TERM $SQL_PID 2>/dev/null || true
  exit 1
fi
echo "SQL Server is ready. Starting API..."

# 3. Start the API
cd /app
APP_DLL="$(ls /app/*.runtimeconfig.json | head -n1 | sed 's/\.runtimeconfig\.json$/.dll/')"
echo "Starting $APP_DLL"
dotnet "$APP_DLL" &
APP_PID=$!

trap 'kill -TERM $APP_PID $SQL_PID 2>/dev/null' SIGTERM SIGINT

wait -n $SQL_PID $APP_PID
EXIT_CODE=$?
kill -TERM $APP_PID $SQL_PID 2>/dev/null || true
exit $EXIT_CODE