@echo off
setlocal
rem Builds vehicle_materials.dll and its offline configuration test, then runs it.
pushd "%~dp0"
where cl >nul 2>nul
if errorlevel 1 call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
where cl >nul 2>nul
if errorlevel 1 ( echo ERROR: Visual Studio x64 C++ build tools are required. & popd & exit /b 1 )
if not exist build mkdir build
cl /nologo /O2 /MT /W3 /EHsc /LD /Fo"build\\" /Fd"build\\" /Fe"build\vehicle_materials.dll" ..\vehicle_materials\vehicle_materials.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
cl /nologo /O2 /MT /W3 /EHsc /Fo"build\\" /Fe"build\vehicle_materials_config_test.exe" vehicle_materials_config_test.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
build\vehicle_materials_config_test.exe "%~dp0build\vehicle_materials.dll"
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
