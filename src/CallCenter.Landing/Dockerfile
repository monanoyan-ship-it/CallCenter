# ============================================================
# CallCenter Landing — Multi-Stage Docker Build
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/CallCenter.Shared/CallCenter.Shared.csproj CallCenter.Shared/
COPY src/CallCenter.Landing/CallCenter.Landing.csproj CallCenter.Landing/
RUN dotnet restore CallCenter.Landing/CallCenter.Landing.csproj

COPY src/CallCenter.Shared/ CallCenter.Shared/
COPY src/CallCenter.Landing/ CallCenter.Landing/
RUN dotnet publish CallCenter.Landing/CallCenter.Landing.csproj \
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

ENTRYPOINT ["dotnet", "CallCenter.Landing.dll"]
