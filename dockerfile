FROM microsoft/dotnet:2.1-runtime-alpine

# Metadata indicating an image maintainer.
MAINTAINER pmiller@microsoft.com

COPY . /app
WORKDIR /app

# Sets a command or process that will run each time a container is run from the new image.
ENTRYPOINT ["dotnet", "dropdownloadcore.dll"]