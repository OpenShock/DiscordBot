# Build from the repo root:  docker build -f OpenShock.Activity.Api/Dockerfile -t openshock/activity-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first for layer-cached restore.
COPY OpenShock.DiscordBot.Shared/OpenShock.DiscordBot.Shared.csproj OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/OpenShock.Activity.Api.csproj OpenShock.Activity.Api/
RUN dotnet restore OpenShock.Activity.Api/OpenShock.Activity.Api.csproj

# Copy the rest and publish.
COPY OpenShock.DiscordBot.Shared/ OpenShock.DiscordBot.Shared/
COPY OpenShock.Activity.Api/ OpenShock.Activity.Api/
RUN dotnet publish OpenShock.Activity.Api/OpenShock.Activity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenShock.Activity.Api.dll"]
