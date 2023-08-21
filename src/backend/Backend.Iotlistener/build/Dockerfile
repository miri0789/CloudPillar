# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory
WORKDIR /app

# Copy the .csproj file and restore the dependencies
COPY Backend.Iotlistener/*.csproj ./Backend.Iotlistener
COPY Backend.Iotlistener/nuget.config ./Backend.Iotlistener
COPY infra/common/common.csproj ./infra/common

# Copy the rest of the source code
COPY . ./
WORKDIR /app/Backend.Iotlistener
RUN dotnet nuget locals all --clear
RUN dotnet restore

# Build the application for release
RUN dotnet publish -c Release -o /app/out

# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0


ARG DLL
RUN echo $DLL
# Set the entrypoint to run the application
ENTRYPOINT ["dotnet", "${DLL}"]
