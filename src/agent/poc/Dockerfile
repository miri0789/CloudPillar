# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app
RUN pwsh ./publish.ps1
RUN cp ./agent/poc/publish/*.zip /app  

# Second stage: Create the final image with only the required files
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build /app .

# Set the entrypoint for the application
ENTRYPOINT ["dotnet", "jnjiotagent.dll"]