# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY valuation-suite.sln ./
COPY Common/Common.csproj                                    Common/
COPY Core/Domain/Domain.csproj                               Core/Domain/
COPY Core/Application/Application.csproj                     Core/Application/
COPY Infrastructure/Persistance/Persistence.csproj           Infrastructure/Persistance/
COPY Infrastructure/RequestHandlers/RequestHandlers.csproj   Infrastructure/RequestHandlers/
COPY Web/ApiFramework/ApiFramework.csproj                    Web/ApiFramework/
COPY Web/Api/Web.csproj                                      Web/Api/

RUN dotnet restore valuation-suite.sln

COPY . .
RUN dotnet publish Web/Api/Web.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ── Stage 2: Runtime (self-contained ASP.NET + SQLite) ──────────────────────
# No bundled SQL Server: the container is a single API process with a SQLite
# file database (Database:Provider=Sqlite). Simple to deploy (Back4app, Fly.io,
# any container host) — data lives in the mounted /app/data volume.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

# Non-root user; /app/data is the SQLite volume (writable by the app).
RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid 1001 appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# Docker profile: SQLite file database (override with Database__Provider=SqlServer
# + ConnectionStrings__AssessmentDb to point the same image at SQL Server).
ENV Database__Provider=Sqlite
ENV Database__SqlitePath=/app/data/valuationsuite.db

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Web.dll"]
