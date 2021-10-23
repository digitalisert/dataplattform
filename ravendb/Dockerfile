FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY *.cs ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS resourcemodel
WORKDIR /app
COPY --from=build-env /app/out .

FROM ravendb/ravendb:5.2.3-ubuntu.20.04-x64

COPY --from=resourcemodel \
    /app/Digitalisert.Dataplattform.ResourceModel.dll \
    /app/Geohash.dll \
    /app/MoreLinq.dll \
    /app/NetTopologySuite.dll \
    /app/ProjNET.dll \
    /app/SmartFormat.dll \
    /opt/RavenDB/Server/
