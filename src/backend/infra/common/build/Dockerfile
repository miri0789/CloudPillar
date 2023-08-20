# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory
WORKDIR /app

# Copy the .csproj file and restore the dependencies
COPY *.csproj ./
COPY *.nuget.config ./
RUN dotnet restore --configfile "./nuget.config"

# Copy the rest of the source code
COPY . ./

# Build the application for release
RUN dotnet publish -c Release -o out

# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Set the working directory
WORKDIR /app

# Copy the build output from the previous stage
COPY --from=build /app/out .

ARG DLL
# Set the entrypoint to run the application
ENTRYPOINT ["dotnet", "${DLL}"]
