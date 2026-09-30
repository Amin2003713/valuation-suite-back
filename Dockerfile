# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY valuation-suite.sln .
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

# ── Stage 2: Runtime (SQL Server + ASP.NET) ─────────────────────────────────
FROM mcr.microsoft.com/mssql/server:2022-latest AS runtime

USER root

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates libicu70 libssl3 && \
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    bash /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm -rf /tmp/dotnet-install.sh /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN sed -i 's/\r$//' /app/docker-entrypoint.sh && \
    chmod +x /app/docker-entrypoint.sh && \
    chown -R mssql:root /app

USER mssql

EXPOSE 8080

# SQL Server (override the password in your platform's env settings)
ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Express
ENV MSSQL_SA_PASSWORD=ChangeMe_Str0ng!Passw0rd
ENV MSSQL_MEMORY_LIMIT_MB=1536

# .NET app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=ValuationSuite;User Id=sa;Password=ChangeMe_Str0ng!Passw0rd;TrustServerCertificate=True;Encrypt=True"

VOLUME /var/opt/mssql

HEALTHCHECK --interval=30s --timeout=10s --start-period=90s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/docker-entrypoint.sh"]