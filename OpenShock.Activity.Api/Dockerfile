FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5562fd626eec39f6f53218f9b7d7856ab7195d51d846568304fbe85071a3b6e6 AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY Directory.Build.props Directory.Packages.props ./
COPY OpenShock.DiscordBot.Shared/*.csproj ./OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/*.csproj ./OpenShock.Activity.Api/
RUN dotnet restore OpenShock.Activity.Api/OpenShock.Activity.Api.csproj

# Copy sources and publish.
COPY OpenShock.DiscordBot.Shared/ ./OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/ ./OpenShock.Activity.Api/
RUN dotnet publish OpenShock.Activity.Api/OpenShock.Activity.Api.csproj -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:486297ae006e95ee2ee3112a2c03e3d26e41fbbc64b7a29e28e7a18cc36b12bb AS final-api
WORKDIR /app

RUN apk add --no-cache openssl

COPY OpenShock.Activity.Api/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

COPY --from=build /app .

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.Activity.Api.dll"]