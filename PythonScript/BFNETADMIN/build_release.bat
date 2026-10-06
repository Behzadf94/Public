@echo off
pushd "%~dp0"

echo [1/3] Cleaning previous builds...
if exist build rmdir /s /q build
if exist dist rmdir /s /q dist
del /q *.spec 2>nul

echo [2/3] Verifying PyInstaller & Assets...
py -m pip install --upgrade pyinstaller customtkinter pystray pillow psutil

echo [3/3] Compiling Single-File Release Executable...
py -m PyInstaller --onefile --windowed --uac-admin --collect-all customtkinter --add-data "assets;assets" --name "BFNETADMIN" bfnetadmin_engine.py

popd
echo ========================================================
echo  BUILD FINISHED! Run: dist\BFNETADMIN.exe
echo ========================================================
pause
