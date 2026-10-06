@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set "OUTPUT_FILE=bfnetadmin_dump.txt"

echo ========================================================
echo       BFNETADMIN - System & Source Collector
echo ========================================================
echo [*] Gathering system info and source files...

(
    echo ========================================================
    echo  BFNETADMIN SYSTEM & NETWORK DIAGNOSTICS DUMP
    echo  Generated at: %DATE% %TIME%
    echo ========================================================
    echo.
    echo --------------------------------------------------------
    echo 1. OS & PYTHON ENVIRONMENT
    echo --------------------------------------------------------
    ver
    echo.
    echo Python Version:
    python --version 2>&1
    echo.
    echo --------------------------------------------------------
    echo 2. CURRENT NETWORK INTERFACES & CONFIGURATION
    echo --------------------------------------------------------
    ipconfig /all
    echo.
    echo --------------------------------------------------------
    echo 3. CURRENT TCP GLOBAL PARAMETERS
    echo --------------------------------------------------------
    netsh int tcp show global
    echo.
    echo --------------------------------------------------------
    echo 4. CURRENT IP / MTU CONFIGURATION
    echo --------------------------------------------------------
    netsh int ip show subinterfaces
    echo.
    echo --------------------------------------------------------
    echo 5. SOURCE CODES
    echo --------------------------------------------------------
) > "%OUTPUT_FILE%"

:: لیست فایل‌های هدفی که باید جمع‌آوری شوند
set "FILES=main.py network_utils.py command_registry.py base_command.py scanner.py ad_manager.py db_core.py"

for %%F in (%FILES%) do (
    if exist "%%F" (
        (
            echo.
            echo ========================================================
            echo [FILE BEGIN: %%F]
            echo ========================================================
            type "%%F"
            echo.
            echo ========================================================
            echo [FILE END: %%F]
            echo ========================================================
        ) >> "%OUTPUT_FILE%"
        echo  [+] Collected: %%F
    ) else (
        (
            echo.
            echo ========================================================
            echo [FILE: %%F - NOT FOUND]
            echo ========================================================
        ) >> "%OUTPUT_FILE%"
        echo  [-] Not found: %%F
    )
)

echo.
echo [*] Diagnostics and source files dumped successfully to: %OUTPUT_FILE%
echo [*] Opening %OUTPUT_FILE% in Notepad...

notepad.exe "%OUTPUT_FILE%"

endlocal
pause
