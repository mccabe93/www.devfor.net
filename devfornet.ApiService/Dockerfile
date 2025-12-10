# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["devfornet.ApiService/devfornet.ApiService.csproj", "devfornet.ApiService/"]
COPY ["devfornet.db/devfornet.db.csproj", "devfornet.db/"]
COPY ["devfornet.Shared/devfornet.Shared.csproj", "devfornet.Shared/"]
COPY ["devfornet.ServiceDefaults/devfornet.ServiceDefaults.csproj", "devfornet.ServiceDefaults/"]
RUN dotnet restore "./devfornet.ApiService/devfornet.ApiService.csproj"
COPY . .
WORKDIR "/src/devfornet.ApiService"
RUN dotnet build "./devfornet.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./devfornet.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "devfornet.ApiService.dll"]