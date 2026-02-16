@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set MAX_LINES=1000
set current_lines=0
set part_number=1
set output_file=all-code-part1.txt

rem Создаём первый файл с UTF-8 BOM
powershell -NoProfile -Command "'' | Out-File -FilePath '!output_file!' -Encoding UTF8" 2>nul

for /r %%f in (*.cs) do (
    rem Подсчитываем строки в файле через PowerShell (безопасно для UTF-8)
    set file_lines=0
    for /f %%i in ('powershell -NoProfile -Command "(Get-Content -LiteralPath \"%%f\" -Encoding UTF8 -ErrorAction SilentlyContinue | Measure-Object).Count" 2^>nul') do set file_lines=%%i

    rem Считаем общее количество строк с учётом заголовка и разделителя
    set /a lines_to_add=file_lines + 2

    rem Если текущий файл не пустой И добавление превысит лимит — создаём новую часть
    if !current_lines! gtr 0 (
        set /a projected_total=current_lines + lines_to_add
        if !projected_total! gtr !MAX_LINES! (
            set /a part_number+=1
            set output_file=all-code-part!part_number!.txt
            powershell -NoProfile -Command "'' | Out-File -FilePath '!output_file!' -Encoding UTF8" 2>nul
            set current_lines=0
        )
    )

    rem Записываем заголовок
    powershell -NoProfile -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; '----- File: %%~pf%%~nxf -----' | Out-File -FilePath '!output_file!' -Encoding UTF8 -Append" 2>nul

    rem Записываем содержимое файла
    powershell -NoProfile -Command "Get-Content -LiteralPath '%%f' -Encoding UTF8 -Raw -ErrorAction SilentlyContinue | Out-File -FilePath '!output_file!' -Encoding UTF8 -Append" 2>nul

    rem Записываем пустую строку-разделитель
    powershell -NoProfile -Command "'' | Out-File -FilePath '!output_file!' -Encoding UTF8 -Append" 2>nul

    rem Обновляем счётчик строк
    set /a current_lines+=lines_to_add
)

echo.
echo Готово! Создано !part_number! файлов (каждый <=1000 строк, скрипты не разорваны).
echo Первый файл: !output_file!
pause