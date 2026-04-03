# ============================================================
# CallCenter Management — Multi-Stage Docker Build
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarini once kopyala (restore cache icin)
COPY src/CallCenter.Shared/CallCenter.Shared.csproj CallCenter.Shared/
COPY src/CallCenter.Management/CallCenter.Management.csproj CallCenter.Management/
RUN dotnet restore CallCenter.Management/CallCenter.Management.csproj

# Kaynak kodlari kopyala ve publish et
COPY src/CallCenter.Shared/ CallCenter.Shared/
COPY src/CallCenter.Management/ CallCenter.Management/
RUN dotnet publish CallCenter.Management/CallCenter.Management.csproj \
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

ENTRYPOINT ["dotnet", "CallCenter.Management.dll"]
