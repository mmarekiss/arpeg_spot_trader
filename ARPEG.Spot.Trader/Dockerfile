# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy everything else and build app
COPY ./*.sln /source/
COPY ./ARPEG.Spot.Trader/ /source/ARPEG.Spot.Trader/
COPY ./ARPEG.Spot.Trader.Integration/ /source/ARPEG.Spot.Trader.Integration/
COPY ./ARPEG.Spot.Trader.UI/ /source/ARPEG.Spot.Trader.UI/

RUN cd ARPEG.Spot.Trader.Integration
RUN dotnet restore
RUN cd ../ARPEG.Spot.Trader
RUN dotnet restore
RUN cd ../ARPEG.Spot.Trader.UI
RUN dotnet restore

RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:9876
ENV ASPNET_ENVIRONMENT=Production
ENV TZ=Europe/Prague
ENTRYPOINT ["dotnet", "ARPEG.Spot.Trader.UI.dll"]