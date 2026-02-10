namespace SafonovMyBaby
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
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
            await DisplayAlert("В разработке", "Кнопка 3 (Говорить + Инбокс) скоро будет работать", "ОК");
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
                string filePath = "/storage/emulated/0/Documents/Hi/01 Список дел на сейчас.md";

                // Проверяем, существует ли файл
                string[]? existingLines = null;
                if (File.Exists(filePath))
                {
                    existingLines = await File.ReadAllLinesAsync(filePath);
                }

                // Ищем строку "Инбоксы:" и вставляем текст после неё
                List<string> newLines = new List<string>();

                if (existingLines != null)
                {
                    bool foundInboxes = false;
                    for (int i = 0; i < existingLines.Length; i++)
                    {
                        newLines.Add(existingLines[i]);

                        // Если нашли строку "Инбоксы:" (ТОЛЬКО это слово)
                        if (existingLines[i].Trim() == "Инбоксы:")
                        {
                            foundInboxes = true;
                            // Вставляем наш текст после этой строки
                            newLines.Add(text);
                        }
                    }

                    // Если не нашли строку "Инбоксы:", добавляем её в конец
                    if (!foundInboxes)
                    {
                        newLines.Add("Инбоксы:");
                        newLines.Add(text);
                    }
                }
                else
                {
                    // Файл не существует, создаём новый
                    newLines.Add("Инбоксы:");
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
