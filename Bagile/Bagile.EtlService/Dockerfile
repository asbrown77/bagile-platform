FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Inject version information
ARG APP_VERSION=dev
ENV APP_VERSION=${APP_VERSION}
LABEL org.opencontainers.image.version=${APP_VERSION}

# Restore dependencies
COPY ["Bagile/Bagile.EtlService/Bagile.EtlService.csproj", "Bagile/Bagile.EtlService/"]
COPY ["Bagile/Bagile.Infrastructure/Bagile.Infrastructure.csproj", "Bagile/Bagile.Infrastructure/"]
RUN dotnet restore "Bagile/Bagile.EtlService/Bagile.EtlService.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/Bagile/Bagile.EtlService"
RUN dotnet publish "Bagile.EtlService.csproj" -c Release -p:Version=${APP_VERSION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bagile.EtlService.dll"]

HEALTHCHECK NONE
