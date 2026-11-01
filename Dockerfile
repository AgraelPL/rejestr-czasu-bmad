# Application image (AD-20): built by the deploy workflow (story 4.8), locally with
#   docker build --build-arg APP_IMAGE_TAG=<tag> -t rejestr-czasu:<tag> .
# Contains only the published application: no appsettings.Local.json, .env or data (AD-13, .dockerignore).

# Build stage: the SDK pinned in global.json.
FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1
WORKDIR /src
COPY global.json ./
COPY src/RejestrCzasu/RejestrCzasu.csproj src/RejestrCzasu/
RUN dotnet restore src/RejestrCzasu/RejestrCzasu.csproj
COPY src/RejestrCzasu/ src/RejestrCzasu/
RUN dotnet publish src/RejestrCzasu -c Release --no-restore -o /app/publish -p:UseAppHost=false

# Final stage: the regular ASP.NET runtime image (Ubuntu, with ICU and tzdata; not chiseled) for pl-PL and
# Europe/Warsaw (AD-6). No TZ: the application converts to Europe/Warsaw itself.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_IMAGE_TAG=local
ENV ASPNETCORE_HTTP_PORTS=8080 \
    APP_IMAGE_TAG=${APP_IMAGE_TAG}
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
# Non-root user defined by the .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "RejestrCzasu.dll"]
