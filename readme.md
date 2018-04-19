Steps to create container (should script)

dotnet publish -c Release
cd bin\Release\netcoreapp2.0\publish\
docker build . -t paulgmiller/vstsdropdownloadcore
docker push paulgmiller/vstsdropdownloadcore

In the release definition you need to set.

In agent phase (allow scripts ot acces oauth)

In task
ImageNmae
paulgmiller/vstsdropdownloadcore
Environment vaiables 
relativepath=<whateveer subpath>
vstspat= $(System.AccessToken)