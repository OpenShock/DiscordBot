FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /app
COPY publish .
RUN apk add --no-cache icu-libs

ENTRYPOINT ["dotnet", "OpenShock.DiscordBot.dll"]