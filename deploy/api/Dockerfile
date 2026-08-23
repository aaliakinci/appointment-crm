FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build

ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.0-local
ARG VCS_REF=unknown

WORKDIR /source
COPY . .
RUN dotnet restore AppointmentCrm.slnx --locked-mode --disable-parallel --verbosity minimal -m:1
RUN dotnet publish src/server/AppointmentCrm.Api/AppointmentCrm.Api.csproj \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false \
    --verbosity minimal \
    -m:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-alpine3.23@sha256:4d5339ac9814f1a033a09e664bcf159e9fb386c89b6e7917b3dca7254e656027 AS runtime

ARG VERSION=0.0.0-local
ARG VCS_REF=unknown

LABEL org.opencontainers.image.title="Appointment CRM API" \
    org.opencontainers.image.version="$VERSION" \
    org.opencontainers.image.revision="$VCS_REF" \
    org.opencontainers.image.source="https://github.com/aaliakinci/appointment_crm"

WORKDIR /app
RUN apk add --no-cache \
    krb5-libs=1.22.1-r0 \
    tzdata=2026c-r0 \
    && test -r /usr/share/zoneinfo/Europe/Istanbul \
    && mkdir -p /var/lib/appointment-crm/keys \
    && chown -R $APP_UID:$APP_UID /var/lib/appointment-crm
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --quiet --spider http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AppointmentCrm.Api.dll"]
