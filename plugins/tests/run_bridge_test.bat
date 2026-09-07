@echo off
setlocal
rem Builds workshop_bridge.dll, the stub hook bridge_child.dll and the offline
rem test, then runs the test. Exit code 0 = all checks passed.
pushd "%~dp0"
where cl >nul 2>nul
if errorlevel 1 call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
where cl >nul 2>nul
if errorlevel 1 ( echo ERROR: Visual Studio x64 C++ build tools are required. & popd & exit /b 1 )
if not exist build mkdir build
cl /nologo /O2 /MT /W3 /EHsc /LD /Fo"build\\" /Fd"build\\" /Fe"build\workshop_bridge.dll" ..\workshop_bridge\workshop_bridge.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
cl /nologo /O2 /MT /W3 /EHsc /LD /Fo"build\\" /Fd"build\\" /Fe"build\bridge_child.dll" bridge_child.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
cl /nologo /O2 /MT /W3 /EHsc /Fo"build\\" /Fe"build\workshop_bridge_test.exe" workshop_bridge_test.cpp /link kernel32.lib
if errorlevel 1 ( popd & exit /b 1 )
build\workshop_bridge_test.exe "%~dp0build\workshop_bridge.dll" "%~dp0build\bridge_child.dll"
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
