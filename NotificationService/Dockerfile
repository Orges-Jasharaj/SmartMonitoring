FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8083
ENV ASPNETCORE_URLS=http://+:8083

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
COPY ["SmartMonitoring.Shared/SmartMonitoring.Shared.csproj", "SmartMonitoring.Shared/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"

COPY . .
RUN dotnet publish "NotificationService/NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
