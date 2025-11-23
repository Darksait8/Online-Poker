@echo off
chcp 65001 >nul
echo ========================================
echo   Создание архива проекта для передачи
echo ========================================
echo.

REM Проверяем наличие 7-Zip
where 7z >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Используется 7-Zip для создания архива...
    echo.
    
    REM Создаем архив с исключением ненужных папок
    7z a -tzip "online-poker-project.zip" ^
        "Assets" ^
        "ProjectSettings" ^
        "Packages" ^
        "Server" ^
        "*.md" ^
        ".gitignore" ^
        -xr!"Library" ^
        -xr!"Temp" ^
        -xr!"Logs" ^
        -xr!"build" ^
        -xr!"obj" ^
        -xr!"bin" ^
        -xr!"UserSettings"
    
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ✅ Архив успешно создан: online-poker-project.zip
        echo.
        for %%A in ("online-poker-project.zip") do echo Размер архива: %%~zA байт
    ) else (
        echo ❌ Ошибка при создании архива!
    )
) else (
    echo 7-Zip не найден. Используется встроенный ZIP...
    echo.
    
    REM Используем PowerShell для создания архива
    powershell -Command "& { $files = @('Assets', 'ProjectSettings', 'Packages', 'Server'); Get-ChildItem -Path . -Include *.md,.gitignore -Recurse | ForEach-Object { $files += $_.FullName }; Compress-Archive -Path $files -DestinationPath 'online-poker-project.zip' -Force }"
    
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ✅ Архив успешно создан: online-poker-project.zip
    ) else (
        echo ❌ Ошибка при создании архива!
        echo.
        echo Попробуйте вручную:
        echo 1. Выделите папки: Assets, ProjectSettings, Packages, Server
        echo 2. ПКМ - Отправить - Сжатая ZIP-папка
    )
)

echo.
echo ========================================
echo   Что включено в архив:
echo   ✅ Assets/
echo   ✅ ProjectSettings/
echo   ✅ Packages/
echo   ✅ Server/
echo   ✅ Документация (*.md)
echo.
echo   Что исключено:
echo   ❌ Library/ (генерируется Unity)
echo   ❌ Temp/ (временные файлы)
echo   ❌ build/ (скомпилированная игра)
echo   ❌ Logs/ (логи)
echo ========================================
echo.
pause

