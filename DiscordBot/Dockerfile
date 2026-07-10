FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY OpenShock.DiscordBot.Shared/*.csproj ./OpenShock.DiscordBot.Shared/
COPY DiscordBot/*.csproj ./DiscordBot/
RUN dotnet restore DiscordBot/DiscordBot.csproj

# Copy sources and publish.
COPY OpenShock.DiscordBot.Shared/ ./OpenShock.DiscordBot.Shared/
COPY DiscordBot/ ./DiscordBot/
RUN dotnet publish DiscordBot/DiscordBot.csproj -c Release -o /publish --no-restore


FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]