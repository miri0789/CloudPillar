FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh  | sh

# Set the working directory
WORKDIR /src
ARG DLL
# Copy the .csproj file and restore the dependencies
COPY backend/${DLL}/*.csproj backend/${DLL}/
COPY backend/Infra/Common/Common.csproj backend/Infra/Common/
COPY nuget.config backend/${DLL}/
COPY nuget.config backend/Infra/Common/
ARG FEED_ACCESSTOKEN
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{\"endpointCredentials\": [{\"endpoint\":\"https://pkgs.dev.azure.com/BiosenseWebsterIs/CloudPillar/_packaging/CloudPillarFeed/nuget/v3/index.json\", \"username\":\"docker\", \"password\":\"${FEED_ACCESSTOKEN}\"}]}"
RUN dotnet nuget locals all --clear
RUN dotnet restore backend/${DLL}/${DLL}.csproj
COPY . .
WORKDIR /src/backend/${DLL}

# Build the application for release
RUN dotnet build ${DLL}.csproj -c Release -o /app/build
FROM build AS publish
RUN dotnet publish ${DLL}.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine3.18-amd64 AS base
WORKDIR /app
# Use the ARG PORT value to set environment variables if PORT is not an empty string
ARG PORT
RUN echo "image port: $PORT"
EXPOSE $PORT
ENV DOTNET_RUNNING_IN_CONTAINER=true ASPNETCORE_URLS=http://+:8081
# EXPOSE ${PORT}
# ENV DOTNET_RUNNING_IN_CONTAINER=true ASPNETCORE_URLS=http://+:{$PORT}
 

RUN apk add --no-cache icu-libs
COPY --from=publish /app/publish .
ARG DLL
ENV DLL_NAME=${DLL}.dll
RUN echo "Building for $DLL_NAME"
ENTRYPOINT dotnet $DLL_NAME

