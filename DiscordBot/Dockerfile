FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build
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


FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine@sha256:216f4e2027da6ae806e0bc4b448669ac0faa00125908e308f31dd70598e58136
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]