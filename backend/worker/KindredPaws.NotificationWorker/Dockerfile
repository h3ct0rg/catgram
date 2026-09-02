# Build context is ./backend so this can also copy the shared Contracts project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/KindredPaws.Contracts/KindredPaws.Contracts.csproj shared/KindredPaws.Contracts/
COPY worker/KindredPaws.NotificationWorker/KindredPaws.NotificationWorker.csproj worker/KindredPaws.NotificationWorker/
RUN dotnet restore worker/KindredPaws.NotificationWorker/KindredPaws.NotificationWorker.csproj

COPY shared/KindredPaws.Contracts/ shared/KindredPaws.Contracts/
COPY worker/KindredPaws.NotificationWorker/ worker/KindredPaws.NotificationWorker/
RUN dotnet publish worker/KindredPaws.NotificationWorker/KindredPaws.NotificationWorker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KindredPaws.NotificationWorker.dll"]
