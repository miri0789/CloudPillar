# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory
WORKDIR /app

# Copy the .csproj file and restore the dependencies
COPY backend/Backend.BlobStreamer/*.csproj backend/Backend.BlobStreamer/
COPY nuget.config backend/Backend.BlobStreamer/

COPY . .
WORKDIR /src/backend/Backend.BlobStreamer
RUN dotnet nuget locals all --clear
RUN dotnet restore

# Build the application for release
RUN dotnet build Backend.BlobStreamer.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Backend.BlobStreamer.csproj -c Release -o /app/publish


# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend.BlobStreamer.dll"]
