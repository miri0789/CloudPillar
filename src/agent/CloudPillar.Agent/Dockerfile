# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install the cred provider
# By default, Nano Server runs as ContainerUser. In order to install the cred provider as ContainerUser, an explicit temp directory must be created
WORKDIR /temp
ENV TMP=C:\temp
RUN Invoke-WebRequest https://aka.ms/install-artifacts-credprovider.ps1 -OutFile installcredprovider.ps1; `
    .\installcredprovider.ps1; `
    del installcredprovider.ps1

WORKDIR /src


COPY agent/CloudPillar.Agent/*.csproj agent/CloudPillar.Agent/
COPY nuget.config agent/CloudPillar.Agent/

COPY . .
WORKDIR /src/agent/CloudPillar.Agent
RUN dotnet nuget locals all --clear


ARG FEED_ACCESSTOKEN
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{`"endpointCredentials`": [{`"endpoint`":`"https://pkgs.dev.azure.com/BiosenseWebsterIs/CloudPillar/_packaging/CloudPillarFeed/nuget/v3/index.json`", `"username`":`"docker`", `"password`":`"${FEED_ACCESSTOKEN}`"}]}"
RUN echo $VSS_NUGET_EXTERNAL_FEED_ENDPOINTS
RUN dotnet restore
RUN dotnet publish -c Release -o /app
RUN pwsh ./publish.ps1


# Second stage: Create the final image with only the required files
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build /app .
COPY --from=build ./src/agent/CloudPillar.Agent/publish/cloudpillar.zip .

# Set the entrypoint for the application
ENTRYPOINT ["dotnet", "jnjiotagent.dll"]