docker-compose -f docker-compose.yml down
dotnet publish src/ProxyTor.csproj -c Release -f netcoreapp3.1 -o src/publish/ -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true

