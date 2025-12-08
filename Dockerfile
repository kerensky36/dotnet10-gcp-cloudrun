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

# Copy the published app from build stage
COPY --from=build /app/publish .

# Cloud Run provides PORT at runtime; default to 8080 if not set
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}

# Set the entrypoint
ENTRYPOINT ["dotnet", "dotnet10-gcp-cloudrun.dll"]
