FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ./src/Scrappy.Downloader/Scrappy.Downloader.csproj ./Scrappy.Downloader/
WORKDIR /src/Scrappy.Downloader
RUN dotnet restore

WORKDIR /src
COPY ./src/ .
WORKDIR /src/Scrappy.Downloader
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
COPY --from=build /app/publish /app
ENTRYPOINT ["dotnet", "/app/Scrappy.Downloader.dll"]