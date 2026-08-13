FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# .NET packages cache
COPY ["HotelReservationSystem.Web/HotelReservationSystem.Web.csproj", "HotelReservationSystem.Web/"]
COPY ["HotelReservationSystem.Application/HotelReservationSystem.Application.csproj", "HotelReservationSystem.Application/"]
COPY ["HotelReservationSystem.Infrastructure/HotelReservationSystem.Infrastructure.csproj", "HotelReservationSystem.Infrastructure/"]
COPY ["HotelReservationSystem.Core/HotelReservationSystem.Core.csproj", "HotelReservationSystem.Core/"]
COPY ["HotelReservationSystem.MCP.Server/HotelReservationSystem.MCP.Server.csproj", "HotelReservationSystem.MCP.Server/"]
RUN dotnet restore "HotelReservationSystem.Web/HotelReservationSystem.Web.csproj"

# NPM cache
WORKDIR "/src/HotelReservationSystem.Web"
COPY ["HotelReservationSystem.Web/package.json", "HotelReservationSystem.Web/package-lock.json*", "./"]
RUN npm ci

# publish process
FROM build AS publish
WORKDIR /src
COPY . .
WORKDIR "/src/HotelReservationSystem.Web"
RUN dotnet publish "HotelReservationSystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishReadyToRun=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HotelReservationSystem.Web.dll"]