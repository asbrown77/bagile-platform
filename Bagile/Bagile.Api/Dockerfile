FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Bagile/Bagile.Api/Bagile.Api.csproj", "Bagile/Bagile.Api/"]
COPY ["Bagile/Bagile.Infrastructure/Bagile.Infrastructure.csproj", "Bagile/Bagile.Infrastructure/"]
RUN dotnet restore "Bagile/Bagile.Api/Bagile.Api.csproj"

COPY . .
WORKDIR "/src/Bagile/Bagile.Api"
RUN dotnet publish "Bagile.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bagile.Api.dll"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
