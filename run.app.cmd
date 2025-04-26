@echo off
echo starting the application...
echo logs are at : "%APPDATA%\livingparis\logs"

dotnet run --project LivingParisApp\LivingParisApp.csproj %* /nowarn