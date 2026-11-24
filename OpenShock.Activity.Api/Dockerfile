FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS build
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


FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine@sha256:d39b72ad1534fa0faf52b488bffb0762df7ba269d120f0338713d4dda2a41bf5 AS final-api
WORKDIR /app

RUN apk add --no-cache openssl

COPY OpenShock.Activity.Api/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

COPY --from=build /app .

ENTRYPOINT ["/bin/ash", "/entrypoint.sh", "OpenShock.Activity.Api.dll"]