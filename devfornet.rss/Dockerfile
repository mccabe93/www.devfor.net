FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG BUILD_CONFIGURATION=Release

COPY ["devfornet.rss/devfornet.rss.csproj", "devfornet.rss/"]
COPY ["devfornet.Shared/devfornet.Shared.csproj", "devfornet.Shared/"]
COPY ["devfornet.ServiceDefaults/devfornet.ServiceDefaults.csproj", "devfornet.ServiceDefaults/"]
COPY ["devfornet.db/devfornet.db.csproj", "devfornet.db/"]

RUN dotnet restore "devfornet.rss/devfornet.rss.csproj"

COPY . .
WORKDIR "/src/devfornet.rss"
RUN dotnet build "devfornet.rss.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "devfornet.rss.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "devfornet.rss.dll"]

