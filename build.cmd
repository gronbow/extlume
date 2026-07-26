@echo off
setlocal

set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: The .NET Framework C# compiler was not found.
  exit /b 1
)

if not exist "%ROOT%artifacts" mkdir "%ROOT%artifacts"
set "SOURCE_LIST=%ROOT%artifacts\app-sources.rsp"
if exist "%SOURCE_LIST%" del /q "%SOURCE_LIST%"
for /r "%ROOT%src" %%F in (*.cs) do >>"%SOURCE_LIST%" echo "%%F"

"%CSC%" ^
  /nologo ^
  /target:winexe ^
  /platform:anycpu ^
  /optimize+ ^
  /out:"%ROOT%artifacts\ExtLume.exe" ^
  /win32icon:"%ROOT%assets\app.ico" ^
  /win32manifest:"%ROOT%src\app.manifest" ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Management.dll ^
  @"%SOURCE_LIST%"

if errorlevel 1 exit /b 1
echo Built: %ROOT%artifacts\ExtLume.exe
exit /b 0
