FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY *.slnx .
COPY DiscordBot/*.csproj ./DiscordBot/
COPY MigrationHelper/*.csproj ./MigrationHelper/
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /publish --no-restore


FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]