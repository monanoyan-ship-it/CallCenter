# ============================================================
# CallCenter API — Multi-Stage Docker Build
# ============================================================
# Build:  dotnet SDK ile publish
# Run:    ASP.NET runtime ile calistir (~200MB)
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarini once kopyala (restore cache icin)
COPY src/CallCenter.Shared/CallCenter.Shared.csproj CallCenter.Shared/
COPY src/CallCenter.Data/CallCenter.Data.csproj CallCenter.Data/
COPY src/CallCenter.Api/CallCenter.Api.csproj CallCenter.Api/
RUN dotnet restore CallCenter.Api/CallCenter.Api.csproj

# Kaynak kodlari kopyala ve publish et
COPY src/CallCenter.Shared/ CallCenter.Shared/
COPY src/CallCenter.Data/ CallCenter.Data/
COPY src/CallCenter.Api/ CallCenter.Api/
RUN dotnet publish CallCenter.Api/CallCenter.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# .NET 8+ image'larinda 'app' kullanicisi built-in gelir
USER app

COPY --from=build /app/publish .

# ASP.NET Core container'da varsayilan port 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CallCenter.Api.dll"]
