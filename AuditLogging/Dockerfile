FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AuditLogging/AuditLogging.csproj", "AuditLogging/"]
COPY ["SmartMonitoring.Shared/SmartMonitoring.Shared.csproj", "SmartMonitoring.Shared/"]
RUN dotnet restore "AuditLogging/AuditLogging.csproj"

COPY . .
RUN dotnet publish "AuditLogging/AuditLogging.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditLogging.dll"]
