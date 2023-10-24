FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ./src/Scrappy.Crawler/Scrappy.Crawler.csproj ./Scrappy.Crawler/
WORKDIR /src/Scrappy.Crawler
RUN dotnet restore

WORKDIR /src
COPY ./src/ .
WORKDIR /src/Scrappy.Crawler
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
COPY --from=build /app/publish /app
ENTRYPOINT ["dotnet", "/app/Scrappy.Crawler.dll"]