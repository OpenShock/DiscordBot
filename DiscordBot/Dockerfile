FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5562fd626eec39f6f53218f9b7d7856ab7195d51d846568304fbe85071a3b6e6 AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY Directory.Build.props Directory.Packages.props ./
COPY OpenShock.DiscordBot.Shared/*.csproj ./OpenShock.DiscordBot.Shared/
COPY DiscordBot/*.csproj ./DiscordBot/
RUN dotnet restore DiscordBot/DiscordBot.csproj

# Copy sources and publish.
COPY OpenShock.DiscordBot.Shared/ ./OpenShock.DiscordBot.Shared/
COPY DiscordBot/ ./DiscordBot/
RUN dotnet publish DiscordBot/DiscordBot.csproj -c Release -o /publish --no-restore


FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine@sha256:8ef0660a5ae5877915bc868a27aabf334c8b14c60aadbdd0fa058d81b87c52b6
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]