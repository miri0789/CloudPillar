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

RUN dotnet publish -c Release -o /app/publish


# Use the official .NET Core Runtime image as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "${DLL}.dll"]

