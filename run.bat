@echo off
setlocal

:: ─── Ir al directorio del script ─────────────────────────────────────────────
cd /d "%~dp0"

set "PROJECT=AppCleanerPro.csproj"
set "BIN_DIR=%~dp0bin\Debug\net10.0-windows"
set "EXE_PATH=%BIN_DIR%\AppCleanerPro.exe"

echo =======================================================
echo          Iniciando AppCleaner Pro...
echo =======================================================

:: ─── 1. Detener procesos previos para evitar bloqueos ────────────────────────
echo [1/3] Verificando instancias anteriores...
taskkill /F /IM AppCleanerPro.exe >nul 2>&1
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'AppCleanerPro' -and $_.Name -match 'dotnet' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1
ping 127.0.0.1 -n 2 >nul

:: ─── 2. Compilar ─────────────────────────────────────────────────────────────
echo [2/3] Compilando proyecto...
dotnet build "%PROJECT%" -c Debug --nologo -v minimal
if errorlevel 1 (
    echo [!] Reintentando con limpieza de temporales...
    taskkill /F /IM AppCleanerPro.exe >nul 2>&1
    if exist obj rmdir /s /q obj
    dotnet build "%PROJECT%" -c Debug --nologo -v minimal
    if errorlevel 1 (
        echo.
        echo [ERROR] La compilacion fallo.
        pause
        exit /b 1
    )
)

:: ─── 3. Firmar digitalmente ──────────────────────────────────────────────────
echo [3/3] Aplicando firma de codigo...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sign.ps1"

:: ─── 4. Lanzar la aplicacion ─────────────────────────────────────────────────
echo.
echo Lanzando AppCleaner Pro...
start "" "%EXE_PATH%"

echo Listo!
ping 127.0.0.1 -n 2 >nul
endlocal
