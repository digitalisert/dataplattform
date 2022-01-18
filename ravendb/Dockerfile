FROM mcr.microsoft.com/dotnet/sdk:5.0 AS resourcemodel
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY *.cs ./
RUN dotnet publish -c Release -o out -r linux-x64 --self-contained

FROM ravendb/ravendb:5.3.1-ubuntu.20.04-x64

COPY --from=resourcemodel \
    /app/out/Digitalisert.Dataplattform.ResourceModel \
    /app/out/Digitalisert.Dataplattform.ResourceModel.dll \
    /app/out/Digitalisert.Dataplattform.ResourceModel.deps.json \
    /app/out/Geohash.dll \
    /app/out/MoreLinq.dll \
    /app/out/NetTopologySuite.dll \
    /app/out/ProjNET.dll \
    /app/out/SmartFormat.dll \
    /opt/RavenDB/Server/

CMD (sleep 5; /opt/RavenDB/Server/Digitalisert.Dataplattform.ResourceModel && echo "ResourceModel initialisert!") & exec /opt/RavenDB/run-raven.sh
