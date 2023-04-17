# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS build
WORKDIR /source

# copy everything else and build app
COPY ./ /source/
RUN dotnet publish -c release -o /app 

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:9876
ENV ASPNET_ENVIRONMENT=Production
ENV TZ=Europe/Prague
ENTRYPOINT ["dotnet", "ARPEG.Spot.Trader.dll"]