# CallCenter PBX Service - Multi-stage Dockerfile
# .NET 10 Worker Service, SIP/RTP portlari acik olmali

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarini kopyala (restore icin)
COPY src/CallCenter.Shared/CallCenter.Shared.csproj src/CallCenter.Shared/
COPY src/CallCenter.PbxService/CallCenter.PbxService.csproj src/CallCenter.PbxService/

# Restore
RUN dotnet restore src/CallCenter.PbxService/CallCenter.PbxService.csproj

# Kaynak kodu kopyala
COPY src/CallCenter.Shared/ src/CallCenter.Shared/
COPY src/CallCenter.PbxService/ src/CallCenter.PbxService/

# Publish
RUN dotnet publish src/CallCenter.PbxService/CallCenter.PbxService.csproj \
    -c Release -o /app/publish --no-restore

# ─── Runtime ───
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

# Ses cache ve kayit dizinleri
RUN mkdir -p /app/audio-cache /app/recordings /app/logs

COPY --from=build /app/publish .

# SIP portlari
EXPOSE 5060/udp
EXPOSE 5060/tcp
EXPOSE 5061/tcp

# RTP port araligi (10000-20000)
EXPOSE 10000-20000/udp

# .NET 10 built-in app user
USER app

ENTRYPOINT ["dotnet", "CallCenter.PbxService.dll"]
