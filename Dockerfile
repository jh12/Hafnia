FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG RELEASE_VERSION
ARG BUILD_CONFIGURATION=Release
WORKDIR /sln

COPY ./*.sln ./
COPY src/Hafnia src/Hafnia
COPY src/Hafnia.Frontend src/Hafnia.Frontend
COPY src/Hafnia.DataAccess src/Hafnia.DataAccess
COPY src/Hafnia.DataAccess.Minio src/Hafnia.DataAccess.Minio
COPY src/Hafnia.DataAccess.MongoDB src/Hafnia.DataAccess.MongoDB

COPY src/Hafnia.Shared/src/Hafnia.DTOs src/Hafnia.Shared/src/Hafnia.DTOs

RUN ls -l .
RUN ls -l src/

# TODO: Fix caching
RUN dotnet restore -a $TARGETARCH

COPY ./src ./src
RUN dotnet build "./src/Hafnia/Hafnia.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./src/Hafnia/Hafnia.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:VersionPrefix=$RELEASE_VERSION


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hafnia.dll"]