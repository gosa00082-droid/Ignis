@echo off
chcp 65001 >nul
if exist all-code.txt del /f /q all-code.txt

rem Создаём пустой файл с BOM через PowerShell
powershell -NoProfile -Command "'' | Out-File -FilePath 'all-code.txt' -Encoding UTF8"

for /r %%f in (*.cs) do (
  echo ----- File: %%~pf%%~nxf ----- >> all-code.txt
  powershell -NoProfile -Command "Get-Content -Raw -Path '%%f' | Out-File -FilePath 'all-code.txt' -Encoding UTF8 -Append"
  echo. >> all-code.txt
)

echo all-code.txt обновлён (UTF-8).
pause
