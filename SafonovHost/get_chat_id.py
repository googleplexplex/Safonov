"""
Скрипт для получения вашего Telegram Chat ID
Запустите его и отправьте любое сообщение вашему боту
"""
import requests
import time

TELEGRAM_BOT_TOKEN = '8101590011:AAELBJXsHdEf2TMp_YQHHabu8mhJzxWdzGw'
API_URL = f"https://api.telegram.org/bot{TELEGRAM_BOT_TOKEN}"


def get_chat_id():
    print("Получаю информацию о боте...")
    response = requests.get(f"{API_URL}/getMe")
    data = response.json()

    if data['ok']:
        bot_info = data['result']
        print(f"✅ Бот найден!")
        print(f"Имя: {bot_info['first_name']}")
        print(f"Username: @{bot_info['username']}")
        print(f"\nСсылка на бота: https://t.me/{bot_info['username']}")
        print("\nОтправьте боту любое сообщение в Telegram...")

    # Ждем обновлений
    print("Ожидаю сообщения (5 секунд)...")
    time.sleep(5)

    # Получаем обновления
    response = requests.get(f"{API_URL}/getUpdates")
    data = response.json()

    if data['ok'] and data['result']:
        last_update = data['result'][-1]
        chat_id = last_update['message']['chat']['id']

        print(f"\n✅ Ваш Chat ID: {chat_id}")
        print(f"\nУстановите переменную окружения:")
        print(f"export TELEGRAM_CHAT_ID={chat_id}")
        print("\nИли добавьте в app.py в строке 24:")
        print(f"TELEGRAM_CHAT_ID = '{chat_id}'")
        print("\nДля Windows PowerShell:")
        print(f"$env:TELEGRAM_CHAT_ID='{chat_id}'")
    else:
        print("\n❌ Нет сообщений. Убедитесь, что:")
        print("1. Вы написали боту в Telegram")
        print("2. Бот вам не отвечал (это нормально)")


if __name__ == '__main__':
    get_chat_id()
