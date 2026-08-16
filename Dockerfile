FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
COPY ["SmartMonitoring.Shared/SmartMonitoring.Shared.csproj", "SmartMonitoring.Shared/"]
RUN dotnet restore "IdentityService/IdentityService.csproj"

COPY . .
RUN dotnet publish "IdentityService/IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
