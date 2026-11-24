FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS build
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


FROM mcr.microsoft.com/dotnet/runtime:11.0-alpine@sha256:f79ce0f256d44492ac098aa861836d1a71b3b178019d7357cc6d1437f3ad78d1
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]