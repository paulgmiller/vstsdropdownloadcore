Steps to create container (should script)
docker build . -t paulgmiller/vstsdropdownloadcore
docker push paulgmiller/vstsdropdownloadcore

In the release definition you need to set.

In agent phase (allow scripts ot acces oauth)

In task
ImageName:
paulgmiller/vstsdropdownloadcore
Environment variables :
relativepath=<whateveer subpath>
vstspat= $(System.AccessToken)