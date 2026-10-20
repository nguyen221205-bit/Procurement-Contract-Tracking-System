@echo off
chcp 65001 >nul
title Procurement Frontend SPA - Port 5173
color 0A
echo ======================================================================
echo    KHỞI ĐỘNG FRONTEND SPA (React 19 + Vite)
echo    Cổng truy cập: http://localhost:5173
echo ======================================================================
echo.

cd /d "%~dp0frontend"
npm run dev
pause
