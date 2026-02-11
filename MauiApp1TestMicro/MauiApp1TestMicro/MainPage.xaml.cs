using MauiApp1TestMicro.Services;

namespace MauiApp1TestMicro
{
    public partial class MainPage : ContentPage
    {
        private readonly ISpeechRecognitionService _speechRecognition;
        private bool _isRecording = false;
        private CancellationTokenSource _cancellationTokenSource;

        public MainPage(ISpeechRecognitionService speechRecognition)
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
                var hasPermission = await _speechRecognition.RequestPermissionAsync();

                if (!hasPermission)
                {
                    await DisplayAlert("Ошибка", "Необходимо разрешение на использование микрофона", "OK");
                    return;
                }

                _isRecording = true;
                _cancellationTokenSource = new CancellationTokenSource();
                RecordButton.Text = "Остановить запись";
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
                await _speechRecognition.StopListeningAsync();
                _cancellationTokenSource?.Cancel();
                ResetRecordingState();
            }
        }

        private void OnClearClicked(object? sender, EventArgs e)
        {
            ResultText.Text = string.Empty;
            StatusLabel.Text = "Нажмите кнопку и начните говорить";
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
            RecordButton.Text = "Начать запись";
            StatusLabel.Text = "Нажмите кнопку и начните говорить";
        }
    }
}
