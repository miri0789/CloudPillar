# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | sh

WORKDIR /src

# Copy only the necessary files for restoring NuGet packages
COPY agent/CloudPillar.Agent/*.csproj agent/CloudPillar.Agent/
COPY nuget.config agent/CloudPillar.Agent/

# Copy the entire source code
COPY . .

WORKDIR /src/agent/CloudPillar.Agent
RUN dotnet nuget locals all --clear

ARG FEED_ACCESSTOKEN
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{\"endpointCredentials\": [{\"endpoint\":\"https://pkgs.dev.azure.com/BiosenseWebsterIs/CloudPillar/_packaging/CloudPillarFeed/nuget/v3/index.json\", \"username\":\"docker\", \"password\":\"${FEED_ACCESSTOKEN}\"}]}"

# Restore NuGet packages and build the application
RUN dotnet restore
RUN dotnet build -c Release -o /app
RUN pwsh ./publish.ps1

# Second stage: Create the final image with only the required files
FROM mcr.microsoft.com/dotnet/runtime:7.0

WORKDIR /app

# Copy the published application
COPY --from=build /app .

# Copy the source code and NuGet packages to the /export directory
COPY --from=build ./src/agent/CloudPillar.Agent/ ./export/
COPY --from=build /root/.nuget/packages/sharedentities ./export/.nuget/packages/sharedentities
COPY --from=build ./src/agent/CloudPillar.Agent/publish/cloudpillar.zip .
# Set the entrypoint for the application
ENTRYPOINT ["dotnet", "CloudPillar.Agent.dll"]
