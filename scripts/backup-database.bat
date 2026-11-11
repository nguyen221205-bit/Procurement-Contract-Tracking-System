@echo off
chcp 65001 >nul
echo ========================================================
echo  TIEN ICH SAO LUU DU LIEU PROCUREMENT_DB (SQL SERVER)
echo ========================================================
dotnet run --no-build --project "%~dp0DbTool\DbTool.csproj" -- backup %*
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Sao luu that bai!
    exit /b %ERRORLEVEL%
)

