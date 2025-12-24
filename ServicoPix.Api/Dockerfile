FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ServicoPix.Api/ServicoPix.Api.csproj", "ServicoPix.Api/"]
COPY ["ServicoPix.Application/ServicoPix.Application.csproj", "ServicoPix.Application/"]
COPY ["ServicoPix.Domain/ServicoPix.Domain.csproj", "ServicoPix.Domain/"]
COPY ["ServicoPix.Infrastructure/ServicoPix.Infrastructure.csproj", "ServicoPix.Infrastructure/"]
RUN dotnet restore "./ServicoPix.Api/ServicoPix.Api.csproj"
COPY . .
WORKDIR "/src/ServicoPix.Api"
RUN dotnet build "./ServicoPix.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ServicoPix.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

WORKDIR /src
RUN dotnet tool restore

WORKDIR /src/ServicoPix.Api
RUN dotnet tool run dotnet-ef migrations bundle --self-contained -r linux-x64 -o /app/efbundle

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY --from=publish /app/efbundle .

ENTRYPOINT ["dotnet", "ServicoPix.Api.dll"]