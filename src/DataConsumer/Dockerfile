#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["src/DataConsumer/DataConsumer.csproj", "src/DataConsumer/"]
COPY ["src/Chuck.Infrastructure/Chuck.Infrastructure.csproj", "src/Chuck.Infrastructure/"]
RUN dotnet restore "src/DataConsumer/DataConsumer.csproj"
COPY . .
WORKDIR "/src/src/DataConsumer"
RUN dotnet build "DataConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataConsumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataConsumer.dll"]