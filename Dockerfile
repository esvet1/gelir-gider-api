# .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY GelirGiderTakip.API.csproj ./
RUN dotnet restore GelirGiderTakip.API.csproj

# Copy everything and publish
COPY . ./
RUN dotnet publish GelirGiderTakip.API.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render uses PORT environment variable
EXPOSE 10000

ENTRYPOINT ["dotnet", "GelirGiderTakip.API.dll"]
