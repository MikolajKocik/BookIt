FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:20-slim AS node-build
WORKDIR /app

COPY HotelReservationSystem.Web/package.json HotelReservationSystem.Web/package-lock.json* ./
RUN npm install

COPY HotelReservationSystem.Web/wwwroot/scss ./wwwroot/scss
COPY HotelReservationSystem.Web/Scripts ./Scripts
COPY HotelReservationSystem.Web/tsconfig.json ./

RUN npx sass wwwroot/scss:wwwroot/scss --no-source-map
RUN npx tsc

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

COPY --from=node-build /app/wwwroot/scss ./HotelReservationSystem.Web/wwwroot/scss
COPY --from=node-build /app/wwwroot/js   ./HotelReservationSystem.Web/wwwroot/js

WORKDIR "/src/HotelReservationSystem.Web"
RUN dotnet restore "HotelReservationSystem.Web.csproj"
RUN dotnet publish "HotelReservationSystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishReadyToRun=true /p:TypeScriptCompileBlocked=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HotelReservationSystem.Web.dll"]
