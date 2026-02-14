# ============================================================
# CallCenter Web — Blazor WASM + Nginx
# ============================================================
# Build:  dotnet SDK ile WASM publish (statik dosyalar)
# Run:    Nginx ile serve + API reverse proxy
# ============================================================

# --- Stage 1: Build WASM ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarini once kopyala (restore cache icin)
COPY src/CallCenter.Shared/CallCenter.Shared.csproj CallCenter.Shared/
COPY src/CallCenter.Web/CallCenter.Web.csproj CallCenter.Web/
RUN dotnet restore CallCenter.Web/CallCenter.Web.csproj

# Kaynak kodlari kopyala ve publish et
COPY src/CallCenter.Shared/ CallCenter.Shared/
COPY src/CallCenter.Web/ CallCenter.Web/
RUN dotnet publish CallCenter.Web/CallCenter.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Nginx ---
FROM nginx:alpine AS runtime

# Blazor WASM statik dosyalari
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Nginx konfigurasyonu (API proxy + SPA fallback)
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
