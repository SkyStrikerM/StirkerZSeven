@echo off
rem StrikerZ Windows 빌드 (명령줄)
rem   사용: build_windows.bat            일반 빌드 → Builds\Windows\StrikerZ.exe
rem         build_windows.bat dev        Development 빌드 (디버그 로그 · 프로파일러 연결)
rem   주의: 같은 프로젝트를 Unity 에디터가 열고 있으면 실행되지 않습니다 (에디터를 먼저 닫거나, 에디터 메뉴 StrikerZ > Build 사용).
chcp 65001 >nul
setlocal

set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe"
set "PROJECT=%~dp0..\.."
set "OUT=Builds\Windows\StrikerZ.exe"
set "LOG=%PROJECT%\Logs\build_windows.log"
set "EXTRA="
if /I "%~1"=="dev" set "EXTRA=-buildDev"

if not exist "%UNITY%" (
  echo [오류] Unity 6000.3.18f1 을 찾을 수 없습니다: %UNITY%
  exit /b 2
)

echo 빌드 중... (로그: %LOG%)
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod StrikerZ.EditorTools.BuildCli.BuildWindows -buildOut "%OUT%" %EXTRA% -logFile "%LOG%"
set "CODE=%ERRORLEVEL%"

if "%CODE%"=="0" (
  echo [성공] %PROJECT%\%OUT%
) else (
  echo [실패] 종료 코드 %CODE% — 로그 확인: %LOG%
  findstr /C:"[BuildCli]" /C:"error CS" "%LOG%"
)
exit /b %CODE%
