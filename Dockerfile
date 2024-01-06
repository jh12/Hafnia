FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG RELEASE_VERSION
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Hafnia.sln .
COPY src/Hafnia/Hafnia.csproj src/src/Hafnia/
COPY src/Hafnia.Frontend/Hafnia.Frontend.csproj src/src/Hafnia.Frontend/
COPY src/Hafnia.DataAccess/Hafnia.DataAccess.csproj src/src/Hafnia.DataAccess/
COPY src/Hafnia.DataAccess.Minio/Hafnia.DataAccess.Minio.csproj src/src/Hafnia.DataAccess.Minio/
COPY src/Hafnia.DataAccess.MongoDB/Hafnia.DataAccess.MongoDB.csproj src/src/Hafnia.DataAccess.MongoDB/

RUN ls -l .
RUN ls -l src/
RUN ls -l src/src/
RUN ls -l src/src/Hafnia

RUN dotnet restore -a $TARGETARCH

COPY ./src ./
WORKDIR "/src/Hafnia"
RUN dotnet build "./Hafnia/Hafnia.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Hafnia.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:VersionPrefix=$RELEASE_VERSION


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hafnia.dll"]