#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
SQL_PID=$!

cd /app
dotnet Web.dll &
APP_PID=$!

trap 'kill -TERM $APP_PID $SQL_PID 2>/dev/null' SIGTERM SIGINT

wait -n $SQL_PID $APP_PID
EXIT_CODE=$?
kill -TERM $APP_PID $SQL_PID 2>/dev/null || true
exit $EXIT_CODE