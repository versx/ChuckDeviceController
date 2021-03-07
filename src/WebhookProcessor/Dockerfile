#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["src/WebhookProcessor/WebhookProcessor.csproj", "src/"]
RUN dotnet restore "src/WebhookProcessor/WebhookProcessor.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "WebhookProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebhookProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebhookProcessor.dll"]
