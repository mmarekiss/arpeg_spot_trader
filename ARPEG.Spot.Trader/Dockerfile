FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
USER app
WORKDIR /app
EXPOSE 9876
EXPOSE 12345

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# copy everything else and build app
COPY ./ARPEG.Spot.Trader/ARPEG.Spot.Trader.csproj/ ARPEG.Spot.Trader/
COPY ./ARPEG.Spot.Trader.Integration/ ARPEG.Spot.Trader.Integration/ 
COPY ./ARPEG.Spot.Trader.UI/ ARPEG.Spot.Trader.UI/

COPY ["ARPEG.Spot.Trader/ARPEG.Spot.Trader.csproj", "TARPEG.Spot.Trader/"]
COPY ["ARPEG.Spot.Trader.Integration/ARPEG.Spot.Trader.Integration.csproj", "ARPEG.Spot.Trader.Integration/"]
COPY ["ARPEG.Spot.Trader.UI/ARPEG.Spot.Trader.UI.csproj", "ARPEG.Spot.Trader.UI/"]
RUN dotnet restore "ARPEG.Spot.Trader.UI/ARPEG.Spot.Trader.UI.csproj"
COPY . .
WORKDIR "/src/ARPEG.Spot.Trader.UI"
RUN dotnet build "ARPEG.Spot.Trader.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# final stage/image

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:9876
ENV ASPNET_ENVIRONMENT=Production
ENV TZ=Europe/Prague
ENTRYPOINT ["dotnet", "ARPEG.Spot.Trader.UI.dll"]