using SafonovMyBaby.Services;

namespace SafonovMyBaby
{
    public partial class MainPage : ContentPage
    {
        private const string DefaultFilePath = "/storage/emulated/0/Documents/Hi/01 Список дел на сейчас.md";
        private const string DefaultSearchText = "Инбоксы:";

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            // Открываем страницу настроек
            await Navigation.PushAsync(new SettingsPage());
        }

        private async void OnButton1Clicked(object? sender, EventArgs e)
        {
            // Кнопка 1: Писать + Инбокс
            string result = await DisplayPromptAsync("Инбокс", "Введите текст:", "ОК", "Отмена", placeholder: "Текст...");

            if (!string.IsNullOrWhiteSpace(result))
            {
                await SaveTextToFile(result);
            }
        }

        private async void OnButton2Clicked(object? sender, EventArgs e)
        {
            // Кнопка 2: Писать + Нейро
            await DisplayAlert("В разработке", "Кнопка 2 (Писать + Нейро) скоро будет работать", "ОК");
        }

        private async void OnButton3Clicked(object? sender, EventArgs e)
        {
            // Кнопка 3: Говорить + Инбокс
            // Получаем сервис распознавания речи через DI
            var speechService = Handler.MauiContext.Services.GetService<ISpeechRecognitionService>();

            if (speechService != null)
            {
                await Navigation.PushAsync(new SpeechPage(speechService));
            }
            else
            {
#if ANDROID
                await DisplayAlert("Ошибка", "Сервис распознавания речи недоступен. Убедитесь, что вы используете устройство на Android.", "OK");
#else
                await DisplayAlert("Ошибка", "Распознавание речи доступно только на Android.", "OK");
#endif
            }
        }

        private async void OnButton4Clicked(object? sender, EventArgs e)
        {
            // Кнопка 4: Говорить + Нейро
            await DisplayAlert("В разработке", "Кнопка 4 (Говорить + Нейро) скоро будет работать", "ОК");
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

                await DisplayAlert("Успех", "Текст сохранён в файл", "ОК");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить файл: {ex.Message}", "ОК");
            }
        }
    }
}
