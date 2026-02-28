@echo off
setlocal

:: --------------------------------------------------------------------------
:: Configuration
:: --------------------------------------------------------------------------
set "SOURCE_DIR=%~dp0nugets-output"
set "NUGET_SOURCE=https://api.nuget.org/v3/index.json"

:: --------------------------------------------------------------------------
:: Validation
:: --------------------------------------------------------------------------

:: Check if the Environment Variable exists
if "%NUGET_ORG_PUSH_KEY%"=="" (
    echo [ERROR] Environment variable NUGET_ORG_PUSH_KEY is not set.
    echo Please set the variable with your API key before running this script.
    exit /b 1
)

:: Check if the output directory exists
if not exist "%SOURCE_DIR%" (
    echo [ERROR] The directory "%SOURCE_DIR%" does not exist.
    exit /b 1
)

:: --------------------------------------------------------------------------
:: Execution
:: --------------------------------------------------------------------------
echo [INFO] API Key found.
echo [INFO] Looking for packages in: %SOURCE_DIR%
echo.

:: Push all .nupkg files. 
:: NOTE: The dotnet CLI automatically detects adjacent .snupkg files 
:: and pushes them along with the .nupkg.
dotnet nuget push "%SOURCE_DIR%\*.nupkg" ^
    --api-key "%NUGET_ORG_PUSH_KEY%" ^
    --source "%NUGET_SOURCE%" ^
    --skip-duplicate

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] There was a problem pushing the packages.
    exit /b %ERRORLEVEL%
)

echo.
echo [SUCCESS] All packages processed.
pause