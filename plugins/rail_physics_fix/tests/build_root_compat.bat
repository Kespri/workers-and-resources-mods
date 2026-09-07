@echo off
setlocal
pushd "%~dp0.."
for /f "usebackq delims=" %%I in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do call "%%I\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build\root_compat mkdir build\root_compat
rem Match the parent TesmioLoader build's C++-only flags (no extra assembler).
cl /nologo /O2 /MT /W3 /EHsc /LD /Fo"build\root_compat\rail_physics_fix.obj" /Fe"build\root_compat\rail_physics_fix.dll" rail_physics_fix.cpp /link kernel32.lib
set "RP_RESULT=%ERRORLEVEL%"
popd
exit /b %RP_RESULT%
