# How to run it:


## Prerequisites:
* store google cloud API credentials in api-credential.json file (in the application root)
* directory wwwroot/uploads must be writable
* the applicatiom must has the permission to create photos.db file in the app root (SQLite database)



## How to run it:
*(in development environment, for production use just change the ASPNETCORE_ENVIRONMENT variable for Production)*



1. Use Docker:

> docker run -p 8000:80 -e "ASPNETCORE_URLS=http://+:80" -e "GOOGLE_APPLICATION_CREDENTIALS=api-credential.json" -e "ASPNETCORE_ENVIRONMENT=Development" -v `pwd`:/app  -w /app  -it --rm microsoft/dotnet

when in docker:
> dotnet restore

> dotnet build

> dotnet run


1. in visual studio code :
just run it as it is because the configuration is already there in .vscode/launch.json file


