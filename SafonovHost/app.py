from flask import Flask, request
import uuid
import shutil
import subprocess
import os

app = Flask(__name__)

# Глобальные переменные
VAULT_PATH = r'C:\Users\BathDuck\source\repos\SafonovMyBaby\SafonovHost\vault'
CLAUDE_COMMAND_TEMPLATE = 'claude -p \'{prompt}\' --allowedTools "Edit Read" --permission-mode acceptEdits --verbose'


@app.route('/neuro', methods=['POST'])
def neuro():
    prompt = request.get_data(as_text=True)

    # Генерируем случайный GUID
    guid = str(uuid.uuid4())

    # Копируем папку vault с суффиксом -<guid>
    vault_copy_name = f'vault-{guid}'
    vault_copy_path = os.path.join(os.path.dirname(VAULT_PATH), vault_copy_name)

    print(f'Hello! {prompt}')
    print(f'Copying vault to: {vault_copy_path}')

    shutil.copytree(VAULT_PATH, vault_copy_path)

    return '', 200

    # Выполняем команду claude внутри скопированной папки
    command = CLAUDE_COMMAND_TEMPLATE.format(prompt=prompt)
    print(f'Executing command in {vault_copy_path}: {command}')

    result = subprocess.run(
        command,
        shell=True,
        cwd=os.path.dirname(VAULT_PATH),
        capture_output=True,
        text=True
    )

    print('STDOUT:', result.stdout)
    print('STDERR:', result.stderr)

    return '', 200


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
