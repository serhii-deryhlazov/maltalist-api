# Use the official .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build

# Copy everything and restore dependencies
COPY . ./
RUN dotnet restore "MaltalistApi.csproj"

# Build and publish
RUN dotnet publish "MaltalistApi.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5023
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose HTTP port
EXPOSE 5023

ENTRYPOINT ["dotnet", "MaltalistApi.dll"]
