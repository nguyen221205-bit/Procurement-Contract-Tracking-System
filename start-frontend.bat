@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "cd '%~dp0frontend'; npm run dev"
