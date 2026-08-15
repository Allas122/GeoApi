FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GeoApi/GeoApi.csproj", "GeoApi/"]
COPY ["GeoApi.Application/GeoApi.Application.csproj", "GeoApi.Application/"]
COPY ["GeoApi.Domain/GeoApi.Domain.csproj", "GeoApi.Domain/"]
COPY ["GeoApi.Infrastructure/GeoApi.Infrastructure.csproj", "GeoApi.Infrastructure/"]
RUN dotnet restore "GeoApi/GeoApi.csproj"
COPY . .
WORKDIR "/src/GeoApi"
RUN dotnet build "./GeoApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GeoApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GeoApi.dll"]
