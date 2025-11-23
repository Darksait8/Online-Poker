# 🔧 Настройка ConnectionManager

Инструкция по настройке ConnectionManager в вашем проекте.

## 🚀 Быстрая настройка (автоматическая)

### Вариант 1: Через ConnectionManagerSetup (РЕКОМЕНДУЕТСЯ)

1. **Откройте любую сцену** (например, MainMenu или Main)

2. **Создайте пустой GameObject:**
   - ПКМ в Hierarchy → Create Empty
   - Назовите его "ConnectionManagerSetup"

3. **Добавьте компонент ConnectionManagerSetup:**
   - Выберите GameObject
   - Add Component → ConnectionManagerSetup

4. **Нажмите Play или используйте Context Menu:**
   - ПКМ на компоненте → "Setup Connection Manager"
   - Или просто запустите игру - настройка произойдет автоматически

5. **Готово!** UI для подключения будет создан автоматически

## 🎨 Ручная настройка

### Шаг 1: Создайте UI элементы

Если у вас уже есть UI, найдите или создайте:

- **Поля ввода (TMP_InputField):**
  - `ServerHostInput` - для IP-адреса сервера
  - `ServerPortInput` - для порта
  - `PlayerNameInput` - для имени игрока
  - `StartingStackInput` - для начального стека

- **Кнопки (Button):**
  - `ConnectButton` - кнопка подключения
  - `DisconnectButton` - кнопка отключения

### Шаг 2: Настройте ConnectionManager

1. **Создайте GameObject** с компонентом `ConnectionManager`

2. **В Inspector назначьте ссылки:**
   - Server Host Input → ваше поле ввода IP
   - Server Port Input → ваше поле ввода порта
   - Player Name Input → ваше поле ввода имени
   - Starting Stack Input → ваше поле ввода стека
   - Connect Button → ваша кнопка подключения
   - Disconnect Button → ваша кнопка отключения
   - Poker Client → компонент PokerClient (создайте если нет)

### Шаг 3: Проверьте настройки

- Убедитесь, что все поля заполнены
- Запустите игру и проверьте подключение

## 📝 Использование в коде

### Программное подключение:

```csharp
var connectionManager = FindObjectOfType<ConnectionManager>();
if (connectionManager != null)
{
    // Установить IP-адрес
    connectionManager.SetServerAddress("192.168.0.121", 8888);
    
    // Подключиться
    connectionManager.ConnectToServer();
}
```

### Получить текущие настройки:

```csharp
string host = connectionManager.GetServerHost();
int port = connectionManager.GetServerPort();
```

## 🔍 Проверка работы

1. **Запустите сервер** (в папке Server: `dotnet run`)

2. **В Unity:**
   - Откройте сцену с ConnectionManager
   - Введите IP сервера (например: `localhost` или `192.168.0.121`)
   - Введите порт: `8888`
   - Нажмите "Подключиться"

3. **Проверьте Console:**
   - Должно появиться: `🔌 Подключен к серверу...`
   - Или ошибка, если сервер не запущен

## 🐛 Решение проблем

### ConnectionManager не находит UI элементы

**Решение:**
- Используйте `ConnectionManagerSetup` для автоматической настройки
- Или убедитесь, что UI элементы названы правильно:
  - `ServerHostInput`
  - `ServerPortInput`
  - `PlayerNameInput`
  - `StartingStackInput`
  - `ConnectButton`
  - `DisconnectButton`

### Кнопки не работают

**Решение:**
- Проверьте, что кнопки назначены в Inspector
- Убедитесь, что PokerClient создан и назначен

### Не подключается к серверу

**Решение:**
1. Проверьте, что сервер запущен
2. Проверьте IP-адрес (используйте `localhost` для локального сервера)
3. Проверьте порт (должен быть `8888`)
4. Проверьте файрвол

## 💡 Советы

1. **Используйте ConnectionManagerSetup** - это самый простой способ
2. **Сохраняйте настройки** - ConnectionManager автоматически сохраняет введенные данные
3. **Проверяйте Console** - там будут сообщения о статусе подключения
4. **Для удаленного сервера** - используйте IP-адрес вместо `localhost`

## 📚 Дополнительно

- См. `REMOTE_SERVER_SETUP.md` для настройки удаленного сервера
- См. `ONLINE_SETUP.md` для общей настройки онлайн-игры

---

**Готово! ConnectionManager настроен и готов к использованию! 🎮**

