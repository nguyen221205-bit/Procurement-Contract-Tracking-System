@echo off
chcp 65001 >nul
title Bật Dịch vụ SQL Server

:: Kiem tra quyen Administrator va tu dong yeu cau neu chua co
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Đang yêu cầu quyền Administrator để khởi động SQL Server...
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0""", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B
)

echo ======================================================================
echo   ĐANG KHỞI ĐỘNG DỊCH VỤ SQL SERVER (MSSQLSERVER)...
echo ======================================================================
echo.

net start MSSQLSERVER
sc config MSSQLSERVER start= auto

echo.
echo ======================================================================
echo   ĐÃ BẬT SQL SERVER THÀNH CÔNG!
echo   Đã thiết lập SQL Server TỰ ĐỘNG BẬT cùng Windows khi khởi động máy.
echo ======================================================================
echo.
timeout /t 4
exit
