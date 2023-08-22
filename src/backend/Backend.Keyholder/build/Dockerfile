# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory
WORKDIR /app

# Copy the .csproj file and restore the dependencies
COPY backend/Backend.Keyholder/*.csproj ./
COPY nuget.config ./
RUN dotnet nuget locals all --clear
RUN dotnet restore


# Copy the rest of the source code
COPY . ./

# Build the application for release
RUN dotnet build Backend.Keyholder.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Backend.Keyholder.csproj -c Release -o /app/publish


# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend.Keyholder.dll"]
