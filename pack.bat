@echo off
set "targetDir=%~dp0nugets-output"

if exist "%targetDir%" (
    rd /s /q "%targetDir%"
)

mkdir "nugets-output"

cd NextId

dotnet pack -o "../nugets-output"

cd ..

cd NextId.Gen

dotnet pack -o "../nugets-output"

cd ..

cd NextId.Serialization.Json

dotnet pack -o "../nugets-output"

cd ..

