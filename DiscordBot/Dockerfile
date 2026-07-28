FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
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


FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine@sha256:9297ae3050f7e80428142f784a6d016f80bc8b5cd54e7fc1b596935911b1e58e
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]