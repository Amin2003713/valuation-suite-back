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


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

# Non-root runtime user. curl is needed by the HEALTHCHECK below
# (not present in the aspnet base image).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid 1001 appuser \
    && chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false


HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Web.dll"]
