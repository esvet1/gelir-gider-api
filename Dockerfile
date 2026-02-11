# .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}
EXPOSE 10000

ENTRYPOINT ["dotnet", "GelirGiderTakip.API.dll"]
