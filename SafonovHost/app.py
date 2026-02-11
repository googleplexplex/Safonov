from flask import Flask, request
import uuid
import shutil
import subprocess
import os
import threading
import queue
import telegram
from telegram import InlineKeyboardButton, InlineKeyboardMarkup, Update
from telegram.ext import CallbackContext, CommandHandler, CallbackQueryHandler
from github import Github, GithubException
from git import Repo, GitCommandError
from datetime import datetime
import requests
from dotenv import load_dotenv

# Загружаем переменные окружения из .env
load_dotenv()

app = Flask(__name__)

# Глобальные переменные
VAULT_PATH = os.getenv('VAULT_PATH', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'vault'))
CLAUDE_COMMAND_TEMPLATE = 'claude -p \'{prompt}\' --allowedTools "Edit Read Write Bash" --permission-mode bypassPermissions'

# Telegram Bot Configuration
TELEGRAM_BOT_TOKEN = os.getenv('TELEGRAM_BOT_TOKEN')
TELEGRAM_CHAT_ID = os.getenv('TELEGRAM_CHAT_ID')

# GitHub Configuration
GITHUB_TOKEN = os.getenv('GITHUB_TOKEN')
GITHUB_REPO_URL = os.getenv('GITHUB_REPO_URL')
GITHUB_REPO_NAME = os.getenv('GITHUB_REPO_NAME')
GITHUB_REPO_PATH = VAULT_PATH

# Очередь задач
task_queue = queue.Queue()
active_tasks = {}  # guid -> task_info
task_messages = {}  # guid -> message_id

# Инициализация GitHub
github_client = Github(GITHUB_TOKEN)
github_repo = github_client.get_repo(GITHUB_REPO_NAME)

# Инициализация Telegram бота (используем requests для синхронных вызовов)
import requests as http_requests
TELEGRAM_API_URL = f"https://api.telegram.org/bot{TELEGRAM_BOT_TOKEN}"


def send_telegram_message(text, chat_id, reply_markup=None):
    """Отправить сообщение в Telegram через API"""
    if not TELEGRAM_CHAT_ID:
        return None

    data = {
        'chat_id': chat_id,
        'text': text,
        'parse_mode': 'Markdown'
    }

    if reply_markup:
        data['reply_markup'] = reply_markup

    response = http_requests.post(f"{TELEGRAM_API_URL}/sendMessage", json=data)
    return response.json().get('result', {}).get('message_id')


def edit_telegram_message(message_id, text, chat_id, reply_markup=None):
    """Редактировать сообщение в Telegram через API"""
    if not TELEGRAM_CHAT_ID:
        return False

    data = {
        'chat_id': chat_id,
        'message_id': message_id,
        'text': text,
        'parse_mode': 'Markdown'
    }

    if reply_markup:
        data['reply_markup'] = reply_markup

    response = http_requests.post(f"{TELEGRAM_API_URL}/editMessageText", json=data)
    return response.json().get('ok', False)


def send_telegram_document(file_path, chat_id, filename):
    """Отправить документ в Telegram через API"""
    if not TELEGRAM_CHAT_ID:
        return False

    with open(file_path, 'rb') as f:
        files = {'document': (filename, f)}
        data = {'chat_id': chat_id}

        response = http_requests.post(f"{TELEGRAM_API_URL}/sendDocument", files=files, data=data)
        return response.json().get('ok', False)


class GitHubManager:
    """Класс для работы с GitHub репозиторием"""

    def __init__(self, repo_path, repo_url, token):
        self.repo_path = repo_path
        self.repo_url = repo_url
        self.token = token
        self._ensure_repo_initialized()

    def _ensure_repo_initialized(self):
        """Убедиться что репозиторий инициализирован и связан с GitHub"""
        if not os.path.exists(self.repo_path):
            os.makedirs(self.repo_path)

        # Проверяем есть ли .git
        git_dir = os.path.join(self.repo_path, '.git')
        if not os.path.exists(git_dir):
            print(f"Initializing git repo in {self.repo_path}")
            repo = Repo.init(self.repo_path)

            # Настраиваем remote с токеном
            auth_url = self._get_auth_url()
            repo.create_remote('origin', auth_url)

            # Настраиваем стратегию слияния (prefer local changes)
            repo.git.config('pull.rebase', 'false')
            repo.git.config('user.name', 'Safonov Bot')
            repo.git.config('user.email', 'bot@safonov.local')
        else:
            repo = Repo(self.repo_path)
            # Проверяем есть ли remote
            if not repo.remotes:
                auth_url = self._get_auth_url()
                repo.create_remote('origin', auth_url)

            # Настраиваем стратегию слияния если не настроена
            try:
                repo.git.config('pull.rebase', 'false')
            except:
                pass

        # Делаем initial commit если нужно
        repo = Repo(self.repo_path)
        if not repo.heads:
            repo.index.commit("Initial commit")

    def _get_auth_url(self):
        """Получить URL с токеном для авторизации"""
        # Вставляем токен в URL
        return self.repo_url.replace('https://', f'https://oauth2:{self.token}@')

    def pull_changes(self):
        """Получить изменения из GitHub"""
        try:
            repo = Repo(self.repo_path)
            origin = repo.remotes.origin

            # Убеждаемся что git identity настроена
            try:
                repo.git.config('user.name', 'Safonov Bot')
                repo.git.config('user.email', 'bot@safonov.local')
            except:
                pass

            # Pull с авторизацией через токен и разрешением несвязанных историй
            auth_url = self._get_auth_url()
            with repo.git.custom_environment(GIT_ASKPASS='echo', GIT_PASSWORD=''):
                branch = 'main' if 'main' in [h.name for h in repo.heads] else 'master'
                try:
                    repo.git.pull('origin', branch, '--allow-unrelated-histories')
                except Exception as pull_error:
                    # Если не удается смержить, пробуем с --strategy-option=theirs
                    if 'unrelated histories' in str(pull_error):
                        repo.git.pull('origin', branch, '--allow-unrelated-histories', '--strategy-option=theirs')
                    else:
                        raise pull_error
            print("Pulled changes from GitHub")
            return True
        except Exception as e:
            print(f"Error pulling from GitHub: {e}")
            return False

    def push_changes(self, branch_name=None):
        """Отправить изменения в GitHub"""
        try:
            repo = Repo(self.repo_path)
            branch = branch_name or (repo.active_branch.name if repo.active_branch else 'main')

            # Настройка URL с токеном
            auth_url = self._get_auth_url()
            repo.remotes.origin.set_url(auth_url)

            # Push
            repo.git.push('origin', branch)
            print(f"Pushed changes to GitHub (branch: {branch})")
            return True
        except Exception as e:
            print(f"Error pushing to GitHub: {e}")
            return False

    def commit_and_push(self, message, branch=None):
        """Сделать коммит и отправить в GitHub"""
        try:
            repo = Repo(self.repo_path)

            # Убеждаемся что git identity настроена
            try:
                repo.git.config('user.name', 'Safonov Bot')
                repo.git.config('user.email', 'bot@safonov.local')
            except:
                pass

            # Добавляем все изменения
            repo.git.add(A=True)

            # Проверяем есть ли что коммитить
            if repo.is_dirty(untracked_files=True):
                repo.index.commit(message)
                print(f"Committed: {message}")

                # Push в GitHub
                self.push_changes(branch)
                return True
            else:
                print("No changes to commit")
                return False
        except Exception as e:
            print(f"Error in commit_and_push: {e}")
            return False

    def get_commit_diff(self, commit_sha):
        """Получить diff коммита через GitHub API"""
        try:
            commit = github_repo.get_commit(commit_sha)
            files = commit.files

            diff_text = f"Diff для коммита {commit_sha[:7]}\n"
            diff_text += f"Сообщение: {commit.commit.message}\n"
            diff_text += f"Автор: {commit.commit.author.name}\n"
            # Используем last_modified из самого commit (более надёжный способ)
            diff_text += f"Дата: {commit.last_modified}\n\n"
            diff_text += "=" * 80 + "\n\n"

            for file in files:
                diff_text += f"Файл: {file.filename}\n"
                diff_text += f"Статус: {file.status}\n"
                diff_text += f"Изменений: +{file.additions} -{file.deletions}\n\n"

                if file.patch:
                    diff_text += file.patch + "\n"
                diff_text += "=" * 80 + "\n\n"

            return diff_text
        except Exception as e:
            return f"Ошибка получения diff: {e}"

    def revert_commit(self, commit_sha):
        """Отменить коммит через GitHub API"""
        try:
            repo = Repo(self.repo_path)

            # Revert через git
            repo.git.revert(commit_sha, no_edit=True)

            # Push изменений
            self.push_changes()

            return True, f"Коммит {commit_sha[:7]} успешно отменен"
        except Exception as e:
            return False, f"Ошибка отмены коммита: {e}"


# Инициализация менеджера GitHub
github_manager = GitHubManager(
    repo_path=VAULT_PATH,
    repo_url=GITHUB_REPO_URL,
    token=GITHUB_TOKEN
)


class TaskProcessor:
    """Класс для обработки задач из очереди"""

    def __init__(self):
        self.running = True
        self.thread = threading.Thread(target=self.process_queue, daemon=True)
        self.thread.start()

    def process_queue(self):
        """Основной цикл обработки задач"""
        while self.running:
            try:
                task = task_queue.get(timeout=1)
                self.process_task(task)
            except queue.Empty:
                continue
            except Exception as e:
                print(f"Error processing task: {e}")

    def process_task(self, task):
        """Обработка одной задачи"""
        guid = task['guid']
        prompt = task['prompt']

        try:
            # 0. Pull изменений из GitHub перед началом
            github_manager.pull_changes()

            # 1. Отправляем сообщение в Telegram
            message_text = f"Промпт: `{prompt}`\n\nGUID: {guid}"

            if TELEGRAM_CHAT_ID:
                msg_id = send_telegram_message(message_text, TELEGRAM_CHAT_ID)
                if msg_id:
                    task_messages[guid] = msg_id

            # 2. Коммит изменений перед выполнением (если есть)
            github_manager.commit_and_push(f"Pre-task backup - {guid}")

            # 3. Выполняем команду
            print(f"Executing command for {guid}")
            command = CLAUDE_COMMAND_TEMPLATE.format(prompt=prompt)

            result = subprocess.run(
                command,
                shell=True,
                cwd=VAULT_PATH,
                capture_output=True,
                text=True,
                timeout=300  # 5 минут максимум
            )

            print(f"Command completed for {guid}")
            print(f"STDOUT: {result.stdout}")
            print(f"STDERR: {result.stderr}")

            # 4. Коммит изменений после выполнения
            commit_success = github_manager.commit_and_push(guid)

            # 5. Генерируем отчет с diff
            if commit_success:
                # Получаем последний коммит
                repo = Repo(VAULT_PATH)
                last_commit_sha = repo.head.commit.hexsha
                diff_report = github_manager.get_commit_diff(last_commit_sha)
            else:
                diff_report = f"Нет изменений для GUID: {guid}"

            # 6. Обновляем сообщение с кнопками и отчетом
            if TELEGRAM_CHAT_ID and guid in task_messages:
                # Создаем inline keyboard для Telegram API
                keyboard = {
                    'inline_keyboard': [
                        [
                            {'text': 'Отмена', 'callback_data': f"cancel_{guid}"},
                            {'text': 'Повторить', 'callback_data': f"retry_{guid}"}
                        ]
                    ]
                }

                # Редактируем сообщение
                edit_telegram_message(
                    task_messages[guid],
                    message_text,
                    TELEGRAM_CHAT_ID,
                    keyboard
                )

                # Отправляем отчет файлом
                report_filename = f"report_{guid}.txt"
                with open(report_filename, 'w', encoding='utf-8') as f:
                    f.write(diff_report)

                send_telegram_document(
                    report_filename,
                    TELEGRAM_CHAT_ID,
                    f"diff_report_{guid}.txt"
                )

                os.remove(report_filename)

        except subprocess.TimeoutExpired:
            print(f"Task {guid} timed out")
        except Exception as e:
            print(f"Error processing task {guid}: {e}")
            import traceback
            traceback.print_exc()


# Запуск процессора задач
processor = TaskProcessor()


def revert_task(guid):
    """Отменяет изменения коммита с указанным guid"""
    try:
        # Ищем коммит по сообщению (guid)
        repo = Repo(VAULT_PATH)

        # Ищем коммит с нашим guid в сообщении
        for commit in repo.iter_commits():
            if commit.message.strip() == guid:
                print(f"Found commit {commit.hexsha[:7]} for GUID {guid}")
                success, message = github_manager.revert_commit(commit.hexsha)
                return success, message

        return False, f"Коммит с GUID {guid} не найден"
    except Exception as e:
        return False, f"Ошибка отмены: {e}"


@app.route('/neuro', methods=['POST'])
def neuro():
    prompt = request.get_data(as_text=True)

    # Генерируем GUID
    guid = str(uuid.uuid4())

    # Добавляем задачу в очередь
    task = {
        'guid': guid,
        'prompt': prompt
    }
    task_queue.put(task)

    active_tasks[guid] = task

    print(f'Hello! {prompt}')
    print(f'GUID: {guid}')
    print(f'Task added to queue. Queue size: {task_queue.qsize()}')

    return '', 200


# Telegram Bot Handlers
async def button_callback(update: Update, context: CallbackContext) -> None:
    """Обработка нажатий на кнопки"""
    query = update.callback_query
    await query.answer()

    data = query.data
    guid = data.split('_', 1)[1]

    if data.startswith('cancel_'):
        # Отмена - revert коммита
        success, message = revert_task(guid)
        if success:
            await query.edit_message_text(text=f"✅ {message}")
        else:
            await query.edit_message_text(text=f"❌ {message}")

    elif data.startswith('retry_'):
        # Повтор - добавляем задачу обратно в очередь
        if guid in active_tasks:
            task = active_tasks[guid]
            new_guid = str(uuid.uuid4())
            new_task = {
                'guid': new_guid,
                'prompt': task['prompt']
            }
            task_queue.put(new_task)
            active_tasks[new_guid] = new_task

            await query.edit_message_text(
                text=f"🔄 Задача повторяется. Новый GUID: {new_guid}"
            )


# Запуск Telegram бота в отдельном потоке
def run_telegram_bot():
    """Запуск Telegram бота"""
    if not TELEGRAM_BOT_TOKEN:
        print("Telegram bot not configured")
        return

    from telegram.ext import Application

    application = Application.builder().token(TELEGRAM_BOT_TOKEN).build()
    application.add_handler(CallbackQueryHandler(button_callback))

    print("Telegram bot started for callback handling")
    application.run_polling(stop_signals=None)


# Запуск бота в отдельном потоке при старте (для обработки кнопок)
if TELEGRAM_BOT_TOKEN and TELEGRAM_CHAT_ID:
    bot_thread = threading.Thread(target=run_telegram_bot, daemon=True)
    bot_thread.start()


if __name__ == '__main__':
    print("Starting Flask server...")
    print(f"VAULT_PATH: {VAULT_PATH}")
    print(f"GitHub repo: {GITHUB_REPO_NAME}")
    print(f"Telegram bot token configured: {bool(TELEGRAM_BOT_TOKEN)}")
    print(f"Telegram chat_id set: {bool(TELEGRAM_CHAT_ID)}")

    # Проверяем наличие всех необходимых переменных
    missing_vars = []
    if not TELEGRAM_BOT_TOKEN:
        missing_vars.append("TELEGRAM_BOT_TOKEN")
    if not TELEGRAM_CHAT_ID:
        missing_vars.append("TELEGRAM_CHAT_ID")
    if not GITHUB_TOKEN:
        missing_vars.append("GITHUB_TOKEN")
    if not GITHUB_REPO_URL:
        missing_vars.append("GITHUB_REPO_URL")
    if not GITHUB_REPO_NAME:
        missing_vars.append("GITHUB_REPO_NAME")

    if missing_vars:
        print("\n" + "="*60)
        print("⚠️  ВНИМАНИЕ: Отсутствуют переменные окружения!")
        print("="*60)
        print(f"Не установлены: {', '.join(missing_vars)}")
        print("\nСоздайте файл .env на основе .env.example:")
        print("  cp .env.example .env")
        print("\nИ отредактируйте .env, вставив свои значения")
        print("="*60 + "\n")

    app.run(host='0.0.0.0', port=5000, use_reloader=False)
