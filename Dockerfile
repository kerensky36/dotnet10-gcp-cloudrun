FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as a separate layer for caching
COPY ["dotnet10-gcp-cloudrun.csproj", "./"]
RUN dotnet restore "./dotnet10-gcp-cloudrun.csproj"

# Copy remaining files and publish
COPY . .
RUN dotnet publish "dotnet10-gcp-cloudrun.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Running in container
ENV DOTNET_RUNNING_IN_CONTAINER=true
# Bind to the PORT provided by Cloud Run at runtime
ENV ASPNETCORE_URLS="http://+:${PORT}"

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "dotnet10-gcp-cloudrun.dll"]
