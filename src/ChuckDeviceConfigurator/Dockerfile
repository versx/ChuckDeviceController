#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ChuckDeviceConfigurator/ChuckDeviceConfigurator.csproj", "ChuckDeviceConfigurator/"]
COPY ["Libs/ChuckDeviceController.Authorization.Jwt/ChuckDeviceController.Authorization.Jwt.csproj", "Libs/ChuckDeviceController.Authorization.Jwt/"]
COPY ["Libs/ChuckDeviceController.Configuration/ChuckDeviceController.Configuration.csproj", "Libs/ChuckDeviceController.Configuration/"]
COPY ["Libs/ChuckDeviceController.Extensions.Http/ChuckDeviceController.Extensions.Http.csproj", "Libs/ChuckDeviceController.Extensions.Http/"]
COPY ["Libs/ChuckDeviceController.Extensions.Json/ChuckDeviceController.Extensions.Json.csproj", "Libs/ChuckDeviceController.Extensions.Json/"]
COPY ["Libs/ChuckDeviceController.Extensions/ChuckDeviceController.Extensions.csproj", "Libs/ChuckDeviceController.Extensions/"]
COPY ["Libs/ChuckDeviceController.Net/ChuckDeviceController.Net.csproj", "Libs/ChuckDeviceController.Net/"]
COPY ["Libs/ChuckDeviceController.Caching.Memory/ChuckDeviceController.Caching.Memory.csproj", "Libs/ChuckDeviceController.Caching.Memory/"]
COPY ["Libs/ChuckDeviceController.Collections/ChuckDeviceController.Collections.csproj", "Libs/ChuckDeviceController.Collections/"]
COPY ["Libs/ChuckDeviceController.Data.Common/ChuckDeviceController.Data.Common.csproj", "Libs/ChuckDeviceController.Data.Common/"]
COPY ["Libs/ChuckDeviceController.Data/ChuckDeviceController.Data.csproj", "Libs/ChuckDeviceController.Data/"]
COPY ["Libs/ChuckDeviceController.Data.Abstractions/ChuckDeviceController.Data.Abstractions.csproj", "Libs/ChuckDeviceController.Data.Abstractions/"]
COPY ["Libs/ChuckDeviceController.Geometry/ChuckDeviceController.Geometry.csproj", "Libs/ChuckDeviceController.Geometry/"]
COPY ["Libs/ChuckDeviceController.Common/ChuckDeviceController.Common.csproj", "Libs/ChuckDeviceController.Common/"]
COPY ["Libs/ChuckDeviceController.Geometry.Models/ChuckDeviceController.Geometry.Models.csproj", "Libs/ChuckDeviceController.Geometry.Models/"]
COPY ["Libs/ChuckDeviceController.Logging/ChuckDeviceController.Logging.csproj", "Libs/ChuckDeviceController.Logging/"]
COPY ["Libs/ChuckDeviceController.Extensions.Data/ChuckDeviceController.Extensions.Data.csproj", "Libs/ChuckDeviceController.Extensions.Data/"]
COPY ["Libs/ChuckDeviceController.JobControllers/ChuckDeviceController.JobControllers.csproj", "Libs/ChuckDeviceController.JobControllers/"]
COPY ["Libs/ChuckDeviceController.Plugin/ChuckDeviceController.Plugin.csproj", "Libs/ChuckDeviceController.Plugin/"]
COPY ["Libs/ChuckDeviceController.Routing/ChuckDeviceController.Routing.csproj", "Libs/ChuckDeviceController.Routing/"]
COPY ["Libs/ChuckDeviceController.Plugin.Helpers/ChuckDeviceController.Plugin.Helpers.csproj", "Libs/ChuckDeviceController.Plugin.Helpers/"]
COPY ["Libs/ChuckDeviceController.PluginManager/ChuckDeviceController.PluginManager.csproj", "Libs/ChuckDeviceController.PluginManager/"]
COPY ["Libs/ChuckDeviceController.Protos/ChuckDeviceController.Protos.csproj", "Libs/ChuckDeviceController.Protos/"]
RUN dotnet restore "ChuckDeviceConfigurator/ChuckDeviceConfigurator.csproj"
COPY . .
WORKDIR "/src/ChuckDeviceConfigurator"
RUN dotnet build "ChuckDeviceConfigurator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChuckDeviceConfigurator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChuckDeviceConfigurator.dll"]