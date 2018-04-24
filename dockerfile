FROM microsoft/dotnet:2.0-sdk AS build

WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj .

RUN dotnet restore

# copy everything else (just cs?) and build app

COPY *.cs .

RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-runtime-alpine AS runtime

# Metadata indicating an image maintainer.
MAINTAINER pmiller@microsoft.com 

WORKDIR /app
COPY --from=build /app/out .


# Sets a command or process that will run each time a container is run from the new image.
ENTRYPOINT ["dotnet", "dropdownloadcore.dll"]