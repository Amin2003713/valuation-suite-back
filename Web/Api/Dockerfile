# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution + project files first for layer caching
COPY valuation-suite.sln .
COPY src/Common/Common.csproj                 src/Common/Common.csproj
COPY src/Core/Domain/Domain.csproj             src/Core/Domain/Domain.csproj
COPY src/Core/Application/Application.csproj  src/Core/Application/Application.csproj
COPY src/Infrastructure/Persistance/Persistence.csproj  src/Infrastructure/Persistance/Persistence.csproj
COPY src/Infrastructure/RequestHandlers/RequestHandlers.csproj  src/Infrastructure/RequestHandlers/RequestHandlers.csproj
COPY src/Web/ApiFramework/ApiFramework.csproj  src/Web/ApiFramework/ApiFramework.csproj
COPY src/Web/Api/Web.csproj                   src/Web/Api/Web.csproj

RUN dotnet restore valuation-suite.sln -c $BUILD_CONFIGURATION

# Copy the rest of the source and build
COPY src/ ./src/
RUN dotnet publish src/Web/Api/Web.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid 1001 appuser

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Web.Api.dll"]
