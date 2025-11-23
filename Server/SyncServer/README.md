# Сервер синхронизации

Отдельный сервер для синхронизации данных пользователей между всеми игровыми серверами.

## Запуск

### Вариант 1: С аргументами командной строки

```bash
cd Server\SyncServer
dotnet run -- --port 8889 --key "ваш-секретный-ключ"
```

**Важно:** 
- Используйте `--` (два дефиса) после `dotnet run`, чтобы передать аргументы программе
- В Windows используйте обратные слеши `\` в путях

### Вариант 2: С переменными окружения

**Windows (PowerShell):**
```powershell
$env:SYNC_PORT=8889
$env:SYNC_API_KEY="ваш-секретный-ключ"
cd Server/SyncServer
dotnet run
```

**Windows (CMD):**
```cmd
set SYNC_PORT=8889
set SYNC_API_KEY=ваш-секретный-ключ
cd Server\SyncServer
dotnet run
```

**Linux/Mac:**
```bash
export SYNC_PORT=8889
export SYNC_API_KEY="ваш-секретный-ключ"
cd Server/SyncServer
dotnet run
```

## Аргументы командной строки

- `--port <номер>` - Порт для прослушивания (по умолчанию: 8889)
- `--key <ключ>` - API ключ для безопасности (по умолчанию: "default-key-change-me")
- `--data <путь>` - Путь к файлу данных (по умолчанию: "sync_users.json")

## Примеры

```bash
# Запуск на порту 9999 с кастомным ключом
dotnet run -- --port 9999 --key "my-secret-key-123"

# Запуск с кастомным файлом данных
dotnet run -- --data "C:\Data\users.json" --key "my-key"
```

## Проверка работы

После запуска сервер будет доступен по адресу:
- `http://localhost:8889/users` (GET) - получить всех пользователей
- `http://localhost:8889/users` (PUT) - сохранить пользователей

Для проверки используйте curl:
```bash
curl -H "X-API-Key: ваш-ключ" http://localhost:8889/users
```

## Безопасность

⚠️ **Обязательно измените API ключ по умолчанию!**

Используйте сложный ключ:
```bash
# Linux/Mac
openssl rand -hex 32

# Windows (PowerShell)
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
```

## Развертывание

См. `CLOUD_SYNC_SETUP.md` для инструкций по развертыванию на хостинге.

