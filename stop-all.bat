@echo off
chcp 65001 >nul
title Dừng Hệ thống Đấu thầu
color 0C

echo ======================================================================
echo   ĐANG DỪNG CÁC TIẾN TRÌNH HỆ THỐNG ĐẤU THẦU...
echo ======================================================================
echo.

echo Đang giải phóng cổng 5225 (Backend API)...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5225 ^| findstr LISTENING') do (
    echo - Đang tắt tiến trình PID: %%a
    taskkill /F /PID %%a >nul 2>&1
)

echo Đang giải phóng cổng 5173 (Frontend SPA)...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5173 ^| findstr LISTENING') do (
    echo - Đang tắt tiến trình PID: %%a
    taskkill /F /PID %%a >nul 2>&1
)

echo.
echo Đang đóng các cửa sổ dòng lệnh của hệ thống...
taskkill /F /FI "WINDOWTITLE eq Procurement Backend API*" >nul 2>&1
taskkill /F /FI "WINDOWTITLE eq Procurement Frontend SPA*" >nul 2>&1

echo.
echo ======================================================================
echo   ĐÃ DỪNG THÀNH CÔNG TOÀN BỘ TIẾN TRÌNH!
echo ======================================================================
echo.
timeout /t 3 >nul
exit
