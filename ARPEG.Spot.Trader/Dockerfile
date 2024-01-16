# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /source

# copy everything else and build app
COPY ./ARPEG.Spot.Trader/ /source/ARPEG.Spot.Trader/
COPY ./ARPEG.Spot.Trader.Integration/ /source/ARPEG.Spot.Trader.Integration/ 
COPY ./ARPEG.Spot.Trader.UI/ /source/ARPEG.Spot.Trader.UI/

RUN dotnet publish  /source/ARPEG.Spot.Trader.UI -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:9876
ENV ASPNET_ENVIRONMENT=Production
ENV TZ=Europe/Prague
EXPOSE 9876
EXPOSE 12345
ENTRYPOINT ["dotnet", "ARPEG.Spot.Trader.UI.dll"]