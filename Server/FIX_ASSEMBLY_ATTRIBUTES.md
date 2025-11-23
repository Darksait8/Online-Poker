# Исправление ошибки дублирующихся атрибутов сборки

## Проблема

Ошибка: `CS0579: Повторяющийся атрибут "global::System.Runtime.Versioning.TargetFrameworkAttribute"`

Это происходит, когда .NET пытается автоматически генерировать атрибуты сборки, но они уже определены где-то еще.

## Решение

### Шаг 1: Удалите папки obj и bin полностью

**Windows (CMD):**
```cmd
cd C:\Users\артем\Desktop\Online-Poker\Server
rmdir /s /q obj
rmdir /s /q bin
```

**Windows (PowerShell):**
```powershell
cd C:\Users\артем\Desktop\Online-Poker\Server
Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue
```

### Шаг 2: Проверьте файл проекта

Убедитесь, что в `Server/PokerServer.csproj` есть:
```xml
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
</PropertyGroup>
```

### Шаг 3: Пересоберите

```cmd
dotnet build
```

Или сразу запустите:
```cmd
dotnet run
```

## Если проблема сохраняется

Проверьте, нет ли файлов `AssemblyInfo.cs` или других файлов с атрибутами в папке `Server`:

```cmd
dir /s /b *.AssemblyInfo.cs
dir /s /b *AssemblyAttributes.cs
```

Если найдете такие файлы, удалите их или исключите из проекта.

## Альтернативное решение

Если ничего не помогает, можно создать новый проект и скопировать файлы:

1. Создайте новый проект в другой папке
2. Скопируйте все `.cs` файлы
3. Скопируйте настройки из `.csproj`

Но обычно удаление `obj` и `bin` решает проблему.

