## Steps to create container (uses a docker multistage build)
- docker build . -t paulgmiller/vstsdropdownloadcore
- docker push paulgmiller/vstsdropdownloadcore
(obviously you can't push to paulgmiller so choose your own docker namespace)

## In the release definition you need to set.

### In agent phase settings
allow scripts ot access oauth

### In task
- ImageName: paulgmiller/vstsdropdownloadcore
- Volumes: $(System.DefaultWorkingDirectory):/drop
- Environment variables :
  - relativepath=__whatever subpath__
  - vstspat= $(System.AccessToken)
