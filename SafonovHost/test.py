import requests

response = requests.post('http://localhost:5000/neuro', data='Создай новый файл с текстом привет, название его hello')
print(f'Status code: {response.status_code}')
