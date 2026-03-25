# ============================================================
# CallCenter Salon — Multi-Stage Docker Build
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarini once kopyala (restore cache icin)
COPY src/CallCenter.Shared/CallCenter.Shared.csproj CallCenter.Shared/
COPY src/CallCenter.Salon/CallCenter.Salon.csproj CallCenter.Salon/
RUN dotnet restore CallCenter.Salon/CallCenter.Salon.csproj

# Kaynak kodlari kopyala ve publish et
COPY src/CallCenter.Shared/ CallCenter.Shared/
COPY src/CallCenter.Salon/ CallCenter.Salon/
RUN dotnet publish CallCenter.Salon/CallCenter.Salon.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CallCenter.Salon.dll"]
