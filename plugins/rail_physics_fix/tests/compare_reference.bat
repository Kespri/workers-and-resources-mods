@echo off
setlocal
if "%~1"=="" ( echo Usage: compare_reference.bat path\to\1.3.1\rail_physics.cpp & exit /b 1 )
set "RP_REFERENCE=%~f1"
pushd "%~dp0.."
for /f "usebackq delims=" %%I in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do call "%%I\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build mkdir build
cl /nologo /std:c++17 /utf-8 /O2 /MT /W4 /wd4505 /EHsc /FI"%RP_REFERENCE%" /Fo"build\parity_reference.obj" /Fe"build\parity_reference.exe" tests\parity_driver.cpp /link kernel32.lib
if errorlevel 1 goto failed
cl /nologo /std:c++17 /utf-8 /O2 /MT /W4 /wd4505 /EHsc  /Fo"build\parity_current.obj" /Fe"build\parity_current.exe" tests\parity_driver.cpp /link kernel32.lib
if errorlevel 1 goto failed
build\parity_reference.exe > build\parity_reference.txt
if errorlevel 1 goto failed
build\parity_current.exe > build\parity_current.txt
if errorlevel 1 goto failed
fc /b build\parity_reference.txt build\parity_current.txt
if errorlevel 1 goto failed
type build\parity_current.txt
popd
exit /b 0
:failed
popd
exit /b 1
