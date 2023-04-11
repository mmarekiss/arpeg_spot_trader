FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ARPEG.Spot.Trader/ARPEG.Spot.Trader.csproj", "ARPEG.Spot.Trader/"]
RUN dotnet restore "ARPEG.Spot.Trader/ARPEG.Spot.Trader.csproj"
COPY . .
WORKDIR "/src/ARPEG.Spot.Trader"
RUN dotnet build "ARPEG.Spot.Trader.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ARPEG.Spot.Trader.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ARPEG.Spot.Trader.dll"]
