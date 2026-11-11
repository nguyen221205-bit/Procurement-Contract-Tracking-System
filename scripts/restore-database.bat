@echo off
chcp 65001 >nul
echo ========================================================
echo  TIEN ICH PHUC HOI DU LIEU PROCUREMENT_DB (SQL SERVER)
echo ========================================================
dotnet run --no-build --project "%~dp0DbTool\DbTool.csproj" -- restore %*
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Phuc hoi that bai!
    exit /b %ERRORLEVEL%
)

