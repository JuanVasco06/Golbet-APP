@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Open-VisualStudio.ps1"
if errorlevel 1 pause
