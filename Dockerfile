FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y clang zlib1g-dev

COPY BelpostConnectivityAnalyzer/BelpostConnectivityAnalyzer.csproj .
RUN dotnet restore

COPY BelpostConnectivityAnalyzer/ .
RUN dotnet publish -c Release -r linux-x64 --self-contained \
    -o /app/publish


FROM debian:bookworm-slim AS final

ARG APP_UID=1001
RUN apt-get update \
    && apt-get install -y --no-install-recommends libssl3 ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --no-create-home --shell /bin/false --uid "$APP_UID" appuser

WORKDIR /app
RUN mkdir -p /app/data && chown -R appuser:appuser /app/data

COPY --from=build /app/publish/BelpostConnectivityAnalyzer .
COPY --from=build /app/publish/appsettings.json .
COPY --from=build /app/publish/reports.json .

USER appuser
ENTRYPOINT ["/app/BelpostConnectivityAnalyzer"]