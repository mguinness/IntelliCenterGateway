# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source
EXPOSE 80

# copy csproj and restore as distinct layers
COPY ["IntelliCenterGateway/IntelliCenterGateway.csproj", "IntelliCenterGateway/"]
RUN dotnet restore "IntelliCenterGateway/IntelliCenterGateway.csproj" -r linux-arm

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app -r linux-arm --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim-arm32v7
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "IntelliCenterGateway.dll"]
