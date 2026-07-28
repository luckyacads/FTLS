@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply-Remove-FacultyPopup-CloseIcon.ps1" -ProjectRoot "%CD%"
if errorlevel 1 (
    echo.
    echo Update failed. Read the error above.
    pause
    exit /b 1
)
echo.
echo Update completed.
pause
