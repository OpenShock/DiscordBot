FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY OpenShock.DiscordBot.Shared/*.csproj ./OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/*.csproj ./OpenShock.Activity.Api/
RUN dotnet restore OpenShock.Activity.Api/OpenShock.Activity.Api.csproj

# Copy sources and publish.
COPY OpenShock.DiscordBot.Shared/ ./OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/ ./OpenShock.Activity.Api/
RUN dotnet publish OpenShock.Activity.Api/OpenShock.Activity.Api.csproj -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final-api
WORKDIR /app

RUN apk add --no-cache openssl

COPY OpenShock.Activity.Api/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

COPY --from=build /app .

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.Activity.Api.dll"]