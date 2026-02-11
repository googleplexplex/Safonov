using Android.App;
using Android.Content;
using Android.OS;
using Android.Speech;
using Android.Runtime;
using MauiApp1TestMicro.Services;
using Microsoft.Maui.ApplicationModel;

namespace MauiApp1TestMicro.Platforms.Android
{
    public class SpeechRecognitionService : Java.Lang.Object, ISpeechRecognitionService, IRecognitionListener
    {
        private SpeechRecognizer _speechRecognizer;
        private Intent _speechIntent;
        private TaskCompletionSource<string> _recognitionTaskCompletionSource;
        private Context _context;
        private bool _isListening = false;

        public event EventHandler<string> OnSpeechRecognized;
        public event EventHandler<string> OnRecognitionError;

        public SpeechRecognitionService()
        {
            _context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        }

        public async Task<bool> RequestPermissionAsync()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                }

                return status == PermissionStatus.Granted;
            }

            return true;
        }

        public Task<string> StartListeningAsync()
        {
            if (_speechRecognizer == null)
            {
                _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(_context);
                _speechRecognizer.SetRecognitionListener(this);
            }

            _isListening = true;
            _recognitionTaskCompletionSource = new TaskCompletionSource<string>();

            _speechIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            _speechIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);

            // Установка русского языка для распознавания
            _speechIntent.PutExtra(RecognizerIntent.ExtraLanguage, "ru-RU");
            _speechIntent.PutExtra(RecognizerIntent.ExtraLanguagePreference, "ru-RU");
            _speechIntent.PutExtra(RecognizerIntent.ExtraOnlyReturnLanguagePreference, "ru-RU");

            // Увеличиваем таймаут речи для непрерывного распознавания
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, int.MaxValue);
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, int.MaxValue);
            _speechIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, int.MaxValue);

            _speechIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, _context.PackageName);
            _speechIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
            _speechIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

            _speechRecognizer.StartListening(_speechIntent);

            return _recognitionTaskCompletionSource.Task;
        }

        public Task StopListeningAsync()
        {
            _isListening = false;
            _speechRecognizer?.Cancel(); // Используем Cancel вместо StopListening для немедленной остановки
            _recognitionTaskCompletionSource?.TrySetResult(string.Empty);
            return Task.CompletedTask;
        }

        public void OnBeginningOfSpeech()
        {
        }

        public void OnBufferReceived(byte[] buffer)
        {
        }

        public void OnEndOfSpeech()
        {
            // Сбрасываем флаг при завершении речи, но остаемся в режиме прослушивания
            // Флаг будет сброшен только при явной остановке или ошибке
        }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            // Игнорируем ошибки, если мы намеренно остановили запись
            if (!_isListening)
            {
                return;
            }

            _isListening = false;

            var errorMessage = error switch
            {
                SpeechRecognizerError.Audio => "Ошибка записи аудио",
                SpeechRecognizerError.Client => "Ошибка клиента",
                SpeechRecognizerError.InsufficientPermissions => "Недостаточно разрешений",
                SpeechRecognizerError.Network => "Ошибка сети",
                SpeechRecognizerError.NetworkTimeout => "Тайм-аут сети",
                SpeechRecognizerError.NoMatch => "Речь не распознана. Убедитесь, что русский язык установлен в настройках: Настройки → Язык и ввод → Распознавание речи",
                SpeechRecognizerError.RecognizerBusy => "Распознаватель занят",
                SpeechRecognizerError.SpeechTimeout => "Тайм-аут речи. Попробуйте говорить громче",
                _ => "Неизвестная ошибка"
            };

            _recognitionTaskCompletionSource?.TrySetException(new Exception(errorMessage));
            OnRecognitionError?.Invoke(this, errorMessage);
        }

        public void OnReadyForSpeech(Bundle? bundle)
        {
        }

        public void OnResults(Bundle results)
        {
            var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                var text = matches[0];
                _recognitionTaskCompletionSource?.TrySetResult(text);
                OnSpeechRecognized?.Invoke(this, text);
            }
        }

        public void OnPartialResults(Bundle partialResults)
        {
            var matches = partialResults.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                var text = matches[0];
                OnSpeechRecognized?.Invoke(this, text);
            }
        }

        public void OnRmsChanged(float rmsdB)
        {
        }

        public void OnEvent(int eventType, Bundle? bundle)
        {
        }
    }
}
