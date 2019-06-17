FROM microsoft/dotnet:3.0-sdk AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore --runtime linux-x64
COPY *.cs ./
RUN dotnet publish -c Release -r linux-x64 --self-contained -o out

FROM microsoft/dotnet:3.0-runtime-deps AS runtime
MAINTAINER timmydo@microsoft.com 
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["./dropdownloadcore"]