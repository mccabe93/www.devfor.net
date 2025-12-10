# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["devfornet.Web/devfornet.Web.csproj", "devfornet.Web/"]
COPY ["devfornet.Shared/devfornet.Shared.csproj", "devfornet.Shared/"]
COPY ["devfornet.ServiceDefaults/devfornet.ServiceDefaults.csproj", "devfornet.ServiceDefaults/"]
RUN dotnet restore "devfornet.Web/devfornet.Web.csproj"

# copy everything else and build
COPY . .
WORKDIR "/src/devfornet.Web"
RUN dotnet publish "devfornet.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Serve
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "devfornet.Web.dll"]
