# syntax = devthefuture/dockerfile-x

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Copy only project files first for layer-cached restore.
COPY OpenShock.DiscordBot.Shared/OpenShock.DiscordBot.Shared.csproj OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/OpenShock.Activity.Api.csproj OpenShock.Activity.Api/
RUN dotnet restore OpenShock.Activity.Api/OpenShock.Activity.Api.csproj

# Copy the rest and publish.
COPY OpenShock.DiscordBot.Shared/ OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/ OpenShock.Activity.Api/
RUN dotnet publish OpenShock.Activity.Api/OpenShock.Activity.Api.csproj -c Release -o /app --no-restore

# final is the final runtime stage for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final-api
WORKDIR /app

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN apk update && apk add --no-cache openssl

COPY --link --from=build-api /app .
COPY docker/appsettings.Container.json /app/appsettings.Container.json

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.Activity.Api.dll"]
