#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["src/ChuckDeviceController/ChuckDeviceController.csproj", "src/"]
RUN dotnet restore "src/ChuckDeviceController/ChuckDeviceController.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "ChuckDeviceController.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChuckDeviceController.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChuckDeviceController.dll"]
