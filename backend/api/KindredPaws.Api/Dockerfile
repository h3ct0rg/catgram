# Build context is ./backend so this can also copy the shared Contracts project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/KindredPaws.Contracts/KindredPaws.Contracts.csproj shared/KindredPaws.Contracts/
COPY api/KindredPaws.Api/KindredPaws.Api.csproj api/KindredPaws.Api/
RUN dotnet restore api/KindredPaws.Api/KindredPaws.Api.csproj

COPY shared/KindredPaws.Contracts/ shared/KindredPaws.Contracts/
COPY api/KindredPaws.Api/ api/KindredPaws.Api/
RUN dotnet publish api/KindredPaws.Api/KindredPaws.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "KindredPaws.Api.dll"]
