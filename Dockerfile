FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Restore in its own layer so it is cached until a project file changes.
COPY BottleGame.slnx ./
COPY BottleGame.API/BottleGame.API.csproj BottleGame.API/
COPY BottleGame.Application/BottleGame.Application.csproj BottleGame.Application/
COPY BottleGame.Domain/BottleGame.Domain.csproj BottleGame.Domain/
COPY BottleGame.Infrastructure/BottleGame.Infrastructure.csproj BottleGame.Infrastructure/
RUN dotnet restore BottleGame.API/BottleGame.API.csproj

COPY . .
RUN dotnet publish BottleGame.API/BottleGame.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Run as the unprivileged user that the base image provides. It needs a writable folder for the log files.
RUN mkdir -p /app/Logs && chown "$APP_UID" /app/Logs
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "BottleGame.API.dll"]
