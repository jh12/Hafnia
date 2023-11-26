# ====== Production ====== #
FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0-jammy-chiseled as final
WORKDIR /app

# ====== Build image ====== #
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG RELEASE_VERSION
ARG TARGETARCH
WORKDIR /sln

COPY ./*.sln ./

# Copy the main source project files
# TODO: Make generic method work
#COPY src/*/*.csproj ./
#COPY src/*/*/*.csproj ./ # Removes DataAccess directory from path
#RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done

COPY src/Hafnia src/Hafnia
COPY src/Hafnia.Frontend src/Hafnia.Frontend
COPY src/Hafnia.DataAccess src/Hafnia.DataAccess
COPY src/Hafnia.DataAccess.Minio src/Hafnia.DataAccess.Minio
COPY src/Hafnia.DataAccess.MongoDB src/Hafnia.DataAccess.MongoDB

COPY src/Hafnia.Shared/src/Hafnia.DTOs src/Hafnia.Shared/src/Hafnia.DTOs

# Copy the test project files
#COPY test/*/*.csproj ./
#RUN for file in $(ls *.csproj); do mkdir -p test/${file%.*}/ && mv $file test/${file%.*}/; done

RUN ls -l .
RUN ls -l src/

RUN dotnet restore -a $TARGETARCH

#COPY ./test ./test
COPY ./src ./src

RUN dotnet publish "./src/Hafnia/Hafnia.csproj" -a $TARGETARCH --no-restore -c Release -p:VersionPrefix=$RELEASE_VERSION /p:UseAppHost=false -o /app/publish

# ====== Copy to final ====== #
FROM publish AS final

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Hafnia.dll"]