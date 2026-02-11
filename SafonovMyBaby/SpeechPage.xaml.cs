using SafonovMyBaby.Services;

namespace SafonovMyBaby
{
    public partial class SpeechPage : ContentPage
    {
        private const string DefaultFilePath = "/storage/emulated/0/Documents/Hi/01 Список дел на сейчас.md";
        private const string DefaultSearchText = "Инбоксы:";

        private readonly ISpeechRecognitionService _speechRecognition;
        private bool _isRecording = false;
        private CancellationTokenSource _cancellationTokenSource;

        public SpeechPage(ISpeechRecognitionService speechRecognition)
        {
            InitializeComponent();
            _speechRecognition = speechRecognition;

            _speechRecognition.OnSpeechRecognized += OnSpeechRecognized;
            _speechRecognition.OnRecognitionError += OnRecognitionError;
        }

        private async void OnRecordClicked(object? sender, EventArgs e)
        {
            if (!_isRecording)
            {
                // Начинаем запись
                var hasPermission = await _speechRecognition.RequestPermissionAsync();

                if (!hasPermission)
                {
                    await DisplayAlert("Ошибка", "Необходимо разрешение на использование микрофона", "OK");
                    return;
                }

                _isRecording = true;
                _cancellationTokenSource = new CancellationTokenSource();
                RecordButton.BackgroundColor = Colors.Red;
                RecordButton.Text = "⏹";
                StatusLabel.Text = "Слушаю...";
                ResultText.Text = string.Empty;

                try
                {
                    // Запускаем распознавание без ожидания завершения
                    _ = _speechRecognition.StartListeningAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось начать распознавание: {ex.Message}", "OK");
                    ResetRecordingState();
                }
            }
            else
            {
                // Останавливаем запись и сохраняем
                await _speechRecognition.StopListeningAsync();
                _cancellationTokenSource?.Cancel();

                var recognizedText = ResultText.Text;

                // Проверяем, есть ли текст для сохранения
                if (!string.IsNullOrWhiteSpace(recognizedText) &&
                    recognizedText != "Распознанный текст появится здесь...")
                {
                    // Сохраняем текст в файл
                    await SaveTextToFile(recognizedText);

                    // Закрываем страницу и возвращаемся на главную
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Внимание", "Нет распознанного текста для сохранения", "OK");
                    ResetRecordingState();
                }
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            // Останавливаем запись если она активна
            if (_isRecording)
            {
                await _speechRecognition.StopListeningAsync();
                _cancellationTokenSource?.Cancel();
            }

            // Просто закрываем страницу без сохранения
            await Navigation.PopAsync();
        }

        private void OnSpeechRecognized(object? sender, string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Проверяем, что текст содержит кириллицу
                if (!string.IsNullOrWhiteSpace(text) && RussianTextValidator.IsPredominantlyCyrillic(text))
                {
                    var cleanedText = RussianTextValidator.CleanText(text);
                    ResultText.Text = cleanedText;
                }
            });
        }

        private void OnRecognitionError(object? sender, string error)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Не показываем ошибку, если мы остановили запись вручную
                if (_isRecording)
                {
                    await DisplayAlert("Ошибка распознавания", error, "OK");
                    ResetRecordingState();
                }
            });
        }

        private void ResetRecordingState()
        {
            _isRecording = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            RecordButton.BackgroundColor = Color.FromHex("#512BD4");
            RecordButton.Text = "🎤";
            StatusLabel.Text = "Нажмите на микрофон и начните говорить";
        }

        private async Task SaveTextToFile(string text)
        {
            try
            {
                // Получаем путь к файлу из настроек
                string filePath = Preferences.Get("FilePath", DefaultFilePath);

                // Проверяем, существует ли файл
                string[]? existingLines = null;
                if (File.Exists(filePath))
                {
                    existingLines = await File.ReadAllLinesAsync(filePath);
                }

                // Получаем текст для поиска из настроек
                string searchText = Preferences.Get("SearchText", DefaultSearchText);

                // Ищем строку и вставляем текст после неё
                List<string> newLines = new List<string>();

                if (existingLines != null)
                {
                    bool foundSearchLine = false;
                    for (int i = 0; i < existingLines.Length; i++)
                    {
                        newLines.Add(existingLines[i]);

                        // Если нашли строку поиска (ТОЛЬКО это слово)
                        if (existingLines[i].Trim() == searchText.Trim())
                        {
                            foundSearchLine = true;
                            // Вставляем наш текст после этой строки
                            newLines.Add(text);
                        }
                    }

                    // Если не нашли строку поиска, добавляем её в конец
                    if (!foundSearchLine)
                    {
                        newLines.Add(searchText);
                        newLines.Add(text);
                    }
                }
                else
                {
                    // Файл не существует, создаём новый
                    newLines.Add(searchText);
                    newLines.Add(text);
                }

                // Записываем в файл
                await File.WriteAllLinesAsync(filePath, newLines);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить файл: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Очищаем обработчики событий при закрытии страницы
            _speechRecognition.OnSpeechRecognized -= OnSpeechRecognized;
            _speechRecognition.OnRecognitionError -= OnRecognitionError;

            // Останавливаем запись если активна
            if (_isRecording)
            {
                _ = _speechRecognition.StopListeningAsync();
                _cancellationTokenSource?.Cancel();
            }
        }
    }
}
