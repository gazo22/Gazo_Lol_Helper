FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/RiftLens.Domain/RiftLens.Domain.csproj src/RiftLens.Domain/
COPY src/RiftLens.Contracts/RiftLens.Contracts.csproj src/RiftLens.Contracts/
COPY src/RiftLens.Infrastructure/RiftLens.Infrastructure.csproj src/RiftLens.Infrastructure/
COPY src/RiftLens.Api/RiftLens.Api.csproj src/RiftLens.Api/
COPY src/RiftLens.Collector/RiftLens.Collector.csproj src/RiftLens.Collector/
RUN dotnet restore src/RiftLens.Api/RiftLens.Api.csproj && dotnet restore src/RiftLens.Collector/RiftLens.Collector.csproj
COPY src/ src/
ARG PROJECT=RiftLens.Api
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ARG ASSEMBLY=RiftLens.Server
ENV ENTRY=${ASSEMBLY}.dll
ENTRYPOINT ["sh", "-c", "dotnet $ENTRY"]
