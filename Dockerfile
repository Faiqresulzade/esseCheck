# syntax=docker/dockerfile:1

# --- Build mərhələsi ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Əvvəlcə yalnız .csproj-ları köçürürük ki, kod dəyişəndə "restore" təkrar keşlənsin.
COPY EssayCheck.sln ./
COPY src/EssayChecker.Api/EssayChecker.Api.csproj src/EssayChecker.Api/
COPY src/EssayChecker.Application/EssayChecker.Application.csproj src/EssayChecker.Application/
COPY src/EssayChecker.Domain/EssayChecker.Domain.csproj src/EssayChecker.Domain/
COPY src/EssayChecker.Infrastructure/EssayChecker.Infrastructure.csproj src/EssayChecker.Infrastructure/
COPY src/EssayChecker.Persistence/EssayChecker.Persistence.csproj src/EssayChecker.Persistence/

RUN dotnet restore src/EssayChecker.Api/EssayChecker.Api.csproj

COPY src/ src/
RUN dotnet publish src/EssayChecker.Api/EssayChecker.Api.csproj -c Release -o /app/publish --no-restore

# --- Runtime mərhələsi ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Qeyd: Google Play service account JSON-u (GooglePlay:ServiceAccountJsonPath) image-ə
# BAKED edilmir — deploy zamanı volume/bind-mount ilə /app/secrets-ə verilməlidir (bax DEPLOYMENT.md).

ENTRYPOINT ["dotnet", "EssayChecker.Api.dll"]
