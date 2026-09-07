@echo off
setlocal
if "%~1"=="" ( echo Usage: run_tests.bat path\to\SOVIET64.exe & exit /b 1 )
set "RP_TEST_EXE=%~f1"
pushd "%~dp0.."
for /f "usebackq delims=" %%I in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
    call "%%I\VC\Auxiliary\Build\vcvars64.bat" >nul
    set "RP_CLANG=%%I\VC\Tools\Llvm\x64\bin\clang.exe"
)
if not exist build mkdir build
if not exist build\plugins mkdir build\plugins
rem The offline host names build as its base directory; the classic INI lives in plugins\ there.
copy /y rail_physics_fix.ini build\plugins\rail_physics_fix.ini >nul
"%RP_CLANG%" --target=x86_64-pc-windows-msvc -c tests\bridge_harness.S -o build\bridge_harness.obj
if errorlevel 1 goto failed
cl /nologo /std:c++17 /utf-8 /O2 /MT /W4 /wd4505 /EHsc /Fo"build\test_rail_physics_fix.obj" /Fe"build\test_rail_physics_fix.exe" tests\test_rail_physics_fix.cpp build\bridge_harness.obj /link kernel32.lib /INCREMENTAL:NO
if errorlevel 1 goto failed
for %%T in (bad_host disabled beside_dll bad_numeric model allocations guards patch_failures warnings wrong_build changed_replay missing_signature ambiguous_signature lifecycle hook_failure duplicate dll) do (
    build\test_rail_physics_fix.exe %%T "%RP_TEST_EXE%" "%CD%\rail_physics_fix.ini"
    if errorlevel 1 goto failed
)
popd
exit /b 0
:failed
popd
exit /b 1
