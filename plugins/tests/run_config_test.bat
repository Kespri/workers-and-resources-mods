@echo off
setlocal
rem Builds and runs the offline test for my_plugins\tesmio_config.h.
pushd "%~dp0"
where cl >nul 2>nul
if errorlevel 1 call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
where cl >nul 2>nul
if errorlevel 1 ( echo ERROR: Visual Studio x64 C++ build tools are required. & popd & exit /b 1 )
if not exist build mkdir build
cl /nologo /O2 /MT /W3 /EHsc /Fo"build\\" /Fe"build\tesmio_config_test.exe" tesmio_config_test.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
build\tesmio_config_test.exe
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
