# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY agent/poc/* agent/poc/
COPY agent/poc/publish agent/poc/publish
WORKDIR /src/agent/poc

RUN dotnet restore
RUN dotnet publish -c Release -o /app
RUN pwsh ./publish.ps1


# Second stage: Create the final image with only the required files
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build /app .
COPY --from=build ./src/agent/poc/publish/cloudpillar.zip .


# Set the entrypoint for the application
ENTRYPOINT ["dotnet", "jnjiotagent.dll"]