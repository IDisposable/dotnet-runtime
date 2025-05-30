@if not defined _echo @echo off

:: Initializes Visual Studio developer environment. If a build architecture is passed
:: as an argument, it also initializes VC++ build environment and CMakePath.

set "__VCBuildArch="
if /i "%PROCESSOR_ARCHITECTURE%" == "ARM64" (
    if /i "%~1" == "x64"   ( set __VCBuildArch=arm64_amd64 )
    if /i "%~1" == "x86"   ( set __VCBuildArch=arm64_x86 )
    if /i "%~1" == "arm64" ( set __VCBuildArch=arm64 )
    if /i "%~1" == "wasm"  ( set __VCBuildArch=arm64 )
) else (
    if /i "%~1" == "x64"   ( set __VCBuildArch=amd64 )
    if /i "%~1" == "x86"   ( set __VCBuildArch=amd64_x86 )
    if /i "%~1" == "arm64" ( set __VCBuildArch=amd64_arm64 )
    if /i "%~1" == "wasm"  ( set __VCBuildArch=amd64 )
)

:: Default to highest Visual Studio version available that has Visual C++ tools.
::
:: For VS2017 and later, multiple instances can be installed on the same box SxS and VS1*0COMNTOOLS
:: is no longer set as a global environment variable and is instead only set if the user
:: has launched the Visual Studio Developer Command Prompt.
::
:: Following this logic, we will default to the Visual Studio toolset associated with the active
:: Developer Command Prompt. Otherwise, we will query VSWhere to locate the later version of
:: Visual Studio available on the machine. Finally, we will fail the script if no supported
:: instance can be found.

if defined VisualStudioVersion goto :VSDetected

set "__VSWhere=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "__VSCOMNTOOLS="

if not exist "%__VSWhere%" goto :VSWhereMissing

if exist "%__VSWhere%" (
    for /f "tokens=*" %%p in (
        '"%__VSWhere%" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath'
    ) do set __VSCOMNTOOLS=%%p\Common7\Tools
)

if not exist "%__VSCOMNTOOLS%" goto :VSMissing

:: Make sure the current directory stays intact
set "VSCMD_START_DIR=%CD%"

call "%__VSCOMNTOOLS%\VsDevCmd.bat" -no_logo

:: Clean up helper variables
set "__VSWhere="
set "__VSCOMNTOOLS="
set "VSCMD_START_DIR="

:VSDetected
goto :SetVCEnvironment

:VSMissing
echo %__MsgPrefix%Error: Visual Studio 2022 with C++ tools required. ^
Please see https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/windows-requirements.md for build requirements.
exit /b 1

:VSWhereMissing
echo %__MsgPrefix%Error: vswhere couldn not be found in Visual Studio Installer directory at "%__VSWhere%"
exit /b 1

:SetVCEnvironment

if "%__VCBuildArch%"=="" exit /b 0

:: Set the environment for the native build
:: We can set SkipVCEnvInit to skip setting up the MSVC environment from VS and instead assume that the current environment is set up correctly.
:: This is very useful for testing with new MSVC versions that aren't in a VS build yet.
if not defined SkipVCEnvInit (
  if not exist "%VCINSTALLDIR%Auxiliary\Build\vcvarsall.bat" goto :VSMissing
  call "%VCINSTALLDIR%Auxiliary\Build\vcvarsall.bat" %__VCBuildArch%
  if not "%ErrorLevel%"=="0" exit /b 1
)

set "__VCBuildArch="

:: Set CMakePath by evaluating the output from set-cmake-path.ps1.
:: In case of a failure the output is "exit /b 1".
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass -File "%~dp0set-cmake-path.ps1"') do %%a
