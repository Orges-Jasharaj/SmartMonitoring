FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MonitoringService/MonitoringService.csproj", "MonitoringService/"]
COPY ["SmartMonitoring.Shared/SmartMonitoring.Shared.csproj", "SmartMonitoring.Shared/"]
RUN dotnet restore "MonitoringService/MonitoringService.csproj"

COPY . .
RUN dotnet publish "MonitoringService/MonitoringService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonitoringService.dll"]
