FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
# Writable data directory for the SQLite database; created before dropping root so the
# named volume mounted here inherits ownership by the non-root app user.
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TornDiscordBot/TornDiscordBot.csproj", "TornDiscordBot/"]
RUN dotnet restore "TornDiscordBot/TornDiscordBot.csproj"
COPY . .
WORKDIR "/src/TornDiscordBot"
RUN dotnet build "./TornDiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TornDiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TornDiscordBot.dll"]
