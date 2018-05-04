# VSTS Drop downloader for Linux

## Steps to create container (uses a docker multistage build)

- docker build . -t paulgmiller/vstsdropdownloadcore
- docker push paulgmiller/vstsdropdownloadcore
  (obviously you can't push to paulgmiller so choose your own docker namespace)

## In the release definition you need to set.

## In agent phase settings

allow scripts to access oauth

## In task

- ImageName: paulgmiller/vstsdropdownloadcore
- Volumes: $(System.DefaultWorkingDirectory):/drop
- Environment variables to set:
  - RELATIVEPATH: __whatever subpath__
  - VSTSPAT: $(System.AccessToken) (VSTS personal access token)
  - DROPDESTINATION: Destination for drop data (or /drop if unspecified)
  - DROPURL: Drop URL to query
