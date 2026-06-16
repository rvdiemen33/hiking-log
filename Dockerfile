FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/HikingLog.Api/HikingLog.Api.csproj", "src/HikingLog.Api/"]
COPY ["src/HikingLog.Application/HikingLog.Application.csproj", "src/HikingLog.Application/"]
COPY ["src/HikingLog.Infrastructure/HikingLog.Infrastructure.csproj", "src/HikingLog.Infrastructure/"]
COPY ["src/HikingLog.Domain/HikingLog.Domain.csproj", "src/HikingLog.Domain/"]
RUN dotnet restore "src/HikingLog.Api/HikingLog.Api.csproj"
COPY . .
WORKDIR "/src/src/HikingLog.Api"
RUN dotnet publish "HikingLog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HikingLog.Api.dll"]
