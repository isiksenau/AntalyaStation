FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AntalyaStation/AntalyaStation/AntalyaStation.csproj", "AntalyaStation/AntalyaStation/"]
COPY ["AntalyaStation/AntalyaStation.Client/AntalyaStation.Client.csproj", "AntalyaStation/AntalyaStation.Client/"]
RUN dotnet restore "AntalyaStation/AntalyaStation/AntalyaStation.csproj"
COPY . .
WORKDIR "/src/AntalyaStation/AntalyaStation"
RUN dotnet build "./AntalyaStation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AntalyaStation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AntalyaStation.dll"]
