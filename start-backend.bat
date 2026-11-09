@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "cd '%~dp0ProcurementSystem.API'; dotnet run --launch-profile http"
