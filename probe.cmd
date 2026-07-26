@echo off
setlocal

set "ROOT=%~dp0"
call "%ROOT%build.cmd"
if errorlevel 1 exit /b 1

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

"%CSC%" ^
  /nologo ^
  /target:exe ^
  /platform:anycpu ^
  /out:"%ROOT%artifacts\MonitorProbe.exe" ^
  /reference:"%ROOT%artifacts\ExtLume.exe" ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Management.dll ^
  "%ROOT%tools\MonitorProbe.cs"

if errorlevel 1 exit /b 1
"%ROOT%artifacts\MonitorProbe.exe"
exit /b %ERRORLEVEL%
