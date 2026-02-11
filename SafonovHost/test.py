import requests

response = requests.post('http://localhost:5000/neuro', data='Расскажи какие файлы ты видишь в директории где находишься')
print(f'Status code: {response.status_code}')
