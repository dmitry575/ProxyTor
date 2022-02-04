FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o publish/ 

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

COPY --from=build-env /app/publish .

ENTRYPOINT ["dotnet", "ProxyTor.dll"]
