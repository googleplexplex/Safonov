# Настройка окружения

## 1. Установка зависимостей

```bash
pip install -r requirements.txt
```

Или вручную:

```bash
pip install flask python-telegram-bot python-dotenv PyGithub GitPython requests
```

## 2. Настройка переменных окружения

Скопируйте шаблон и отредактируйте его:

```bash
cp .env.example .env
```

Откройте `.env` в текстовом редакторе и заполните свои значения:

### Telegram Bot

1. **TELEGRAM_BOT_TOKEN**
   - Создайте бота через @BotFather в Telegram
   - Скопируйте полученный токен

2. **TELEGRAM_CHAT_ID**
   - Напишите боту @userinfobot в Telegram
   - Он пришлёт ваш Chat ID

### GitHub

3. **GITHUB_TOKEN**
   - Создайте Personal Access Token на GitHub
   - Settings → Developer settings → Personal access tokens → Tokens (classic)
   - Выберите права: `repo` (full control)

4. **GITHUB_REPO_URL**
   - URL вашего GitHub репозитория
   - Пример: `https://github.com/username/repo.git`

5. **GITHUB_REPO_NAME**
   - Имя репозитория в формате `username/repo`
   - Пример: `googleplexplex/SafonovData`

## 3. Запуск

```bash
python app.py
```

Сервер запустится на `http://0.0.0.0:5000`

## Безопасность

⚠️ **ВАЖНО:**
- Файл `.env` добавлен в `.gitignore`
- Никогда не коммитьте `.env` в репозиторий!
- Используйте `.env.example` как шаблон (без секретов)

## Проверка настроек

При запуске приложение покажет:
- Какие переменные загружены
- Какие отсутствуют (если есть)

Пример успешного запуска:
```
Starting Flask server...
VAULT_PATH: /path/to/vault
GitHub repo: username/repo
Telegram bot token configured: True
Telegram chat_id set: True
```
