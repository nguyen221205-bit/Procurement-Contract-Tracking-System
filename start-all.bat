@echo off
chcp 65001 >nul
title Quản lý Khởi động - Hệ thống Đấu thầu
color 0F

echo ======================================================================
echo   HỆ THỐNG MUA SẮM & ĐẤU THẦU ĐIỆN TỬ (PROCUREMENT SYSTEM)
echo   Đang khởi động toàn bộ tiến trình Backend và Frontend...
echo ======================================================================
echo.

set "ROOT_DIR=%~dp0"

echo [1/3] Đang khởi động Backend API (Port 5225)...
start "Procurement Backend API - Port 5225" cmd /c ""%ROOT_DIR%start-backend.bat""

echo [2/3] Đợi Backend sẵn sàng trong 3 giây...
timeout /t 3 /nobreak >nul

echo [3/3] Đang khởi động Frontend SPA (Port 5173)...
start "Procurement Frontend SPA - Port 5173" cmd /c ""%ROOT_DIR%start-frontend.bat""

echo.
echo Đợi Frontend sẵn sàng trong 2 giây...
timeout /t 2 /nobreak >nul

echo.
echo Đang mở trình duyệt truy cập hệ thống: http://localhost:5173 ...
start http://localhost:5173

echo.
echo ======================================================================
echo   HỆ THỐNG ĐÃ ĐƯỢC KHỞI ĐỘNG THÀNH CÔNG!
echo   - Backend API: http://localhost:5225 (Swagger: /swagger)
echo   - Frontend SPA: http://localhost:5173
echo.
echo   Mẹo: Để dừng toàn bộ tiến trình, hãy chạy file stop-all.bat
echo ======================================================================
echo.
echo Cửa sổ này sẽ tự đóng sau 5 giây...
timeout /t 5 >nul
exit
