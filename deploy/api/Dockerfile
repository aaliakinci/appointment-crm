FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23 AS build

WORKDIR /source
COPY . .
RUN dotnet restore AppointmentCrm.slnx --locked-mode --disable-parallel --verbosity minimal -m:1
RUN dotnet publish src/server/AppointmentCrm.Api/AppointmentCrm.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false \
    --verbosity minimal \
    -m:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-alpine3.23 AS runtime

WORKDIR /app
RUN apk add --no-cache krb5-libs=1.22.1-r0
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AppointmentCrm.Api.dll"]
