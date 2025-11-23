# Исправление ошибок сборки

## Проблема: Дублирующиеся атрибуты сборки

Ошибка возникает из-за конфликта автоматически генерируемых атрибутов сборки.

## Решение

### Шаг 1: Очистка проекта

```bash
cd Server
dotnet clean
```

### Шаг 2: Удаление папки obj (если clean не помог)

**Windows:**
```cmd
cd Server
rmdir /s /q obj
rmdir /s /q bin
```

**PowerShell:**
```powershell
cd Server
Remove-Item -Recurse -Force obj, bin
```

### Шаг 3: Пересборка

```bash
dotnet build
```

## Если проблема сохраняется

Проверьте файл `Server/PokerServer.csproj` - в нем должно быть:

```xml
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
```

Если этого нет, добавьте.

## Альтернативное решение

Если ничего не помогает, можно удалить файлы вручную:

1. Закройте все процессы, использующие проект
2. Удалите папки `Server\obj` и `Server\bin`
3. Выполните `dotnet build` заново

