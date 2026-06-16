@echo off
setlocal

dotnet publish ModelTest\ModelTest.csproj ^
  -c Release ^
  -r win-x86 ^
  --self-contained false ^
  -o publish\win-x86-framework

if errorlevel 1 (
  echo.
  echo Publish failed.
  exit /b 1
)

echo.
echo Publish completed: publish\win-x86-framework
