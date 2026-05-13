@echo off
setlocal EnableExtensions

title CorpLynk Log Toplayici

set "COLLECTOR_VERSION=2026.05.12.1"

if defined LOCALAPPDATA (
    set "BASE=%LOCALAPPDATA%\CorpLynk"
) else (
    set "BASE=%USERPROFILE%\AppData\Local\CorpLynk"
)

if defined CORPLYNK_LOG_BASE (
    set "BASE=%CORPLYNK_LOG_BASE%"
)

set "OUTPUT_DIR=%USERPROFILE%\Desktop"
for /f "delims=" %%D in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "[Environment]::GetFolderPath('Desktop')" 2^>nul') do set "OUTPUT_DIR=%%D"

if defined CORPLYNK_LOG_OUTPUT_DIR (
    set "OUTPUT_DIR=%CORPLYNK_LOG_OUTPUT_DIR%"
)

for /f "delims=" %%T in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Date -Format yyyyMMdd-HHmmss" 2^>nul') do set "TS=%%T"
if not defined TS set "TS=%DATE%-%TIME%"
set "TS=%TS::=%"
set "TS=%TS:/=-%"
set "TS=%TS:.=-%"
set "TS=%TS: =0%"

set "PACKAGE_NAME=corplynk-logs-%COMPUTERNAME%-%TS%"
set "WORK=%TEMP%\%PACKAGE_NAME%"
set "ZIP=%OUTPUT_DIR%\%PACKAGE_NAME%.zip"
set "ZIPLOG=%TEMP%\%PACKAGE_NAME%-zip-output.txt"
set "REPORT=%WORK%\copy-report.txt"

echo.
echo CorpLynk log toplama araci
echo ----------------------------------------
echo Bu islem destek ekibine gonderilecek bir zip olusturur.
echo Ses dosyalari, tokenlar ve SIP sifreleri pakete eklenmez.
echo.
echo Kaynak klasor: %BASE%
echo Cikti       : %ZIP%
echo.

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%" >nul 2>&1
)

if exist "%WORK%" (
    rmdir /s /q "%WORK%" >nul 2>&1
)

mkdir "%WORK%" >nul 2>&1
mkdir "%WORK%\logs" >nul 2>&1
mkdir "%WORK%\data" >nul 2>&1

if not exist "%WORK%" (
    echo HATA: Gecici klasor olusturulamadi: %WORK%
    goto EndWithPause
)

echo Package: %PACKAGE_NAME%>"%REPORT%"
echo CreatedAt: %DATE% %TIME%>>"%REPORT%"
echo BasePath: %BASE%>>"%REPORT%"
echo.>>"%REPORT%"

echo [1/6] Log dosyalari aliniyor...
if exist "%BASE%\*.log" (
    for %%F in ("%BASE%\*.log") do call :CopyFile "%%~fF" "%WORK%\logs"
) else (
    echo MISSING: %BASE%\*.log>>"%REPORT%"
)

echo [2/6] Lokal cagri metadata dosyalari aliniyor...
if exist "%BASE%\Data\call-records*.json" (
    for %%F in ("%BASE%\Data\call-records*.json") do call :CopyFile "%%~fF" "%WORK%\data"
) else (
    echo MISSING: %BASE%\Data\call-records*.json>>"%REPORT%"
)
call :CopyFile "%BASE%\Data\recordings.json" "%WORK%\data"

echo [3/6] Ses kaydi klasor listesi hazirlaniyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Join-Path '%BASE%' 'Recordings'; if (Test-Path -LiteralPath $p) { $files = Get-ChildItem -LiteralPath $p -File -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending; 'Folder: ' + $p; 'FileCount: ' + @($files).Count; 'TotalBytes: ' + (($files | Measure-Object Length -Sum).Sum); ''; $files | Select-Object Name,Length,LastWriteTime,FullName | Format-Table -AutoSize | Out-String -Width 240 } else { 'MISSING: ' + $p }" > "%WORK%\recordings-file-list.txt" 2>&1

echo [4/6] Sistem ve uygulama bilgileri aliniyor...
(
    echo CollectorVersion: %COLLECTOR_VERSION%
    echo CreatedAt: %DATE% %TIME%
    echo ComputerName: %COMPUTERNAME%
    echo UserName: %USERNAME%
    echo BasePath: %BASE%
    echo.
    echo Windows:
    ver
    echo.
    echo CorpLynk process:
    tasklist /FI "IMAGENAME eq CorpLynk.exe" /V
    echo.
    echo Dotnet process:
    tasklist /FI "IMAGENAME eq dotnet.exe" /V
) > "%WORK%\system-info.txt" 2>&1

(
    echo HKCU uninstall info:
    reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\CorpLynk_is1" /s
    echo.
    echo HKLM uninstall info:
    reg query "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CorpLynk_is1" /s
    echo.
    echo HKLM WOW6432 uninstall info:
    reg query "HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\CorpLynk_is1" /s
) > "%WORK%\installer-info.txt" 2>&1

powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddDays(-7); Level=1,2} -ErrorAction SilentlyContinue | Where-Object { $_.ProviderName -match 'CorpLynk|\.NET Runtime|Application Error|Windows Error Reporting' -or $_.Message -match 'CorpLynk' } | Select-Object -First 80 TimeCreated,ProviderName,Id,LevelDisplayName,Message | Format-List | Out-String -Width 240" > "%WORK%\windows-application-errors.txt" 2>&1

echo [5/6] Ag bilgileri aliniyor...
ipconfig /all > "%WORK%\network-ipconfig.txt" 2>&1
nslookup cc-api.corplynk.com > "%WORK%\network-dns-api.txt" 2>&1
ping -n 4 cc-api.corplynk.com > "%WORK%\network-ping-api.txt" 2>&1
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'https://cc-api.corplynk.com/health' -TimeoutSec 10; 'HTTP ' + [int]$r.StatusCode; $r.Content } catch { $_.Exception.Message }" > "%WORK%\api-health.txt" 2>&1

echo [6/6] Manifest hazirlaniyor...
(
    echo CorpLynk Support Log Package
    echo CollectorVersion: %COLLECTOR_VERSION%
    echo CreatedAt: %DATE% %TIME%
    echo ComputerName: %COMPUTERNAME%
    echo UserName: %USERNAME%
    echo BasePath: %BASE%
    echo.
    echo Included:
    echo - %BASE%\*.log
    echo - %BASE%\Data\call-records*.json
    echo - %BASE%\Data\recordings.json
    echo - Recordings folder file list only
    echo - Windows Application error events filtered for CorpLynk/.NET
    echo - Basic network/API diagnostics
    echo.
    echo Not included:
    echo - %BASE%\secure-storage.json
    echo - %BASE%\Data\sip-accounts.json
    echo - %BASE%\Recordings\*.wav
    echo - %BASE%\Recordings\*.enc
    echo.
    echo Note:
    echo This package may contain call numbers, call timestamps, file paths, and local machine/network diagnostics.
) > "%WORK%\manifest.txt"

echo Zip olusturuluyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path -LiteralPath '%ZIP%') { Remove-Item -LiteralPath '%ZIP%' -Force }; Compress-Archive -Path '%WORK%\*' -DestinationPath '%ZIP%' -Force" > "%ZIPLOG%" 2>&1

if errorlevel 1 (
    echo.
    echo HATA: Zip olusturulamadi.
    echo Gecici klasor burada kaldi:
    echo %WORK%
    echo.
    type "%ZIPLOG%"
    goto EndWithPause
)

if not exist "%ZIP%" (
    echo.
    echo HATA: Zip komutu hata donmedi ama zip dosyasi bulunamadi.
    echo Beklenen dosya:
    echo %ZIP%
    echo.
    type "%ZIPLOG%"
    goto EndWithPause
)

rmdir /s /q "%WORK%" >nul 2>&1
del "%ZIPLOG%" >nul 2>&1

echo.
echo Tamamlandi.
echo Olusan dosya:
echo %ZIP%
echo.
echo Bu zip dosyasini CorpLynk destek ekibine gonderebilirsiniz.
echo.

goto EndWithPause

:CopyFile
set "SRC=%~1"
set "DST=%~2"
if exist "%SRC%" (
    copy /Y "%SRC%" "%DST%\" >nul 2>&1
    if errorlevel 1 (
        echo FAILED: %SRC%>>"%REPORT%"
    ) else (
        echo COPIED: %SRC%>>"%REPORT%"
    )
) else (
    echo MISSING: %SRC%>>"%REPORT%"
)
exit /b 0

:EndWithPause
if /I not "%CORPLYNK_LOG_NO_PAUSE%"=="1" (
    pause
)
endlocal
