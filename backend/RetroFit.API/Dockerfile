# Backend (ASP.NET Core 8) - višefazni build. Kontekst je folder "backend":
#   docker build -f backend/RetroFit.API/Dockerfile backend

# ---------- 1) Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Prvo samo projektni fajlovi -> "restore" sloj se kešira dok se zavisnosti ne promene.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY RetroFit.Domain/RetroFit.Domain.csproj RetroFit.Domain/
COPY RetroFit.Application/RetroFit.Application.csproj RetroFit.Application/
COPY RetroFit.Infrastructure/RetroFit.Infrastructure.csproj RetroFit.Infrastructure/
COPY RetroFit.API/RetroFit.API.csproj RetroFit.API/
RUN dotnet restore RetroFit.API/RetroFit.API.csproj

COPY RetroFit.Domain/ RetroFit.Domain/
COPY RetroFit.Application/ RetroFit.Application/
COPY RetroFit.Infrastructure/ RetroFit.Infrastructure/
COPY RetroFit.API/ RetroFit.API/
RUN dotnet publish RetroFit.API/RetroFit.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- 2) Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl za health check; folderi za slike i logove pripadaju korisniku bez root prava.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/uploads /app/logs && chown -R $APP_UID /app/uploads /app/logs

COPY --from=build /app/publish .

# Aplikacija NE radi kao root (bezbednost): APP_UID je ugrađeni neprivilegovani korisnik .NET 8 image-a.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Skladiste__Putanja=/app/uploads
EXPOSE 8080
VOLUME ["/app/uploads"]

HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 \
    CMD curl -fsS http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "RetroFit.API.dll"]
