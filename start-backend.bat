@echo off
chcp 65001 >nul
title Procurement Backend API - Port 5225
color 0B
echo ======================================================================
echo    KHỞI ĐỘNG BACKEND API (.NET 8 Clean Architecture)
echo    Cổng lắng nghe: http://localhost:5225
echo    Swagger UI:     http://localhost:5225/swagger
echo ======================================================================
echo.

cd /d "%~dp0"
dotnet run --project ProcurementSystem.API --launch-profile http
pause
