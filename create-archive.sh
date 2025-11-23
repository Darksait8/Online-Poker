#!/bin/bash

echo "========================================"
echo "  Создание архива проекта для передачи"
echo "========================================"
echo ""

# Проверяем наличие zip
if command -v zip &> /dev/null; then
    echo "Используется zip для создания архива..."
    echo ""
    
    # Создаем архив с исключением ненужных папок
    zip -r "online-poker-project.zip" \
        Assets \
        ProjectSettings \
        Packages \
        Server \
        *.md \
        .gitignore \
        -x "Library/*" \
        -x "Temp/*" \
        -x "Logs/*" \
        -x "build/*" \
        -x "Server/bin/*" \
        -x "Server/obj/*" \
        -x "UserSettings/*"
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ Архив успешно создан: online-poker-project.zip"
        ls -lh online-poker-project.zip
    else
        echo "❌ Ошибка при создании архива!"
        exit 1
    fi
else
    echo "❌ zip не установлен!"
    echo "Установите zip: sudo apt-get install zip (Ubuntu/Debian) или brew install zip (Mac)"
    exit 1
fi

echo ""
echo "========================================"
echo "  Что включено в архив:"
echo "  ✅ Assets/"
echo "  ✅ ProjectSettings/"
echo "  ✅ Packages/"
echo "  ✅ Server/"
echo "  ✅ Документация (*.md)"
echo ""
echo "  Что исключено:"
echo "  ❌ Library/ (генерируется Unity)"
echo "  ❌ Temp/ (временные файлы)"
echo "  ❌ build/ (скомпилированная игра)"
echo "  ❌ Logs/ (логи)"
echo "========================================"

