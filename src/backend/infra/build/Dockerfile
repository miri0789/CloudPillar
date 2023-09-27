FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh  | sh

# Set the working directory
WORKDIR /src
ARG DLL
# Copy the .csproj file and restore the dependencies
COPY backend/${DLL}/*.csproj backend/${DLL}/
COPY nuget.config backend/${DLL}/
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

# Use the official .NET Core Runtime image as the base image for the final stage
FROM base AS final
RUN useradd admin
RUN chown -R admin:admin /app
USER admin
WORKDIR /app
COPY --from=publish /app/publish .
ARG DLL
ENV DLL_NAME=${DLL}.dll
RUN echo "Building for $DLL_NAME"
ENTRYPOINT /bin/sh dotnet $DLL_NAME

