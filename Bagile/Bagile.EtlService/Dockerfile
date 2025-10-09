FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Bagile/Bagile.EtlService/Bagile.EtlService.csproj", "Bagile/Bagile.EtlService/"]
COPY ["Bagile/Bagile.Infrastructure/Bagile.Infrastructure.csproj", "Bagile/Bagile.Infrastructure/"]
RUN dotnet restore "Bagile/Bagile.EtlService/Bagile.EtlService.csproj"

COPY . .
WORKDIR "/src/Bagile/Bagile.EtlService"
RUN dotnet publish "Bagile.EtlService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bagile.EtlService.dll"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8081/health || exit 1