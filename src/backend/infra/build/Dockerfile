FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory
WORKDIR /src
ARG DLL

# Copy the .csproj file and restore the dependencies
COPY backend/${DLL}/*.csproj backend/${DLL}/
COPY nuget.config backend/${DLL}/

COPY . .
WORKDIR /src/backend/${DLL}
RUN dotnet nuget locals all --clear
RUN dotnet restore


# Build the application for release
RUN dotnet build ${DLL}.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish


# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "${DLL}.dll"]

