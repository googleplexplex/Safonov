namespace SafonovMyBaby
{
    public partial class SettingsPage : ContentPage
    {
        private const string DefaultFilePath = "/storage/emulated/0/Documents/Hi/01 Список дел на сейчас.md";
        private const string DefaultSearchText = "Инбоксы:";

        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Загружаем текущие значения из Preferences
            string currentFilePath = Preferences.Get("FilePath", DefaultFilePath);
            string currentSearchText = Preferences.Get("SearchText", DefaultSearchText);

            // Устанавливаем значения в элементы управления
            FilePathEntry.Text = currentFilePath;
            SearchTextEntry.Text = currentSearchText;
        }

        private void OnSettingChanged(object? sender, TextChangedEventArgs e)
        {
            // Автоматически сохраняем при каждом изменении текста
            SaveSettings();
        }

        private void SaveSettings()
        {
            // Получаем значения из полей ввода
            string newFilePath = FilePathEntry.Text?.Trim() ?? string.Empty;
            string newSearchText = SearchTextEntry.Text?.Trim() ?? string.Empty;

            // Если поле пустое, используем значение по умолчанию
            if (string.IsNullOrWhiteSpace(newFilePath))
            {
                newFilePath = DefaultFilePath;
            }

            if (string.IsNullOrWhiteSpace(newSearchText))
            {
                newSearchText = DefaultSearchText;
            }

            // Сохраняем в Preferences (без предупреждений, тихо)
            Preferences.Set("FilePath", newFilePath);
            Preferences.Set("SearchText", newSearchText);
        }

        protected override bool OnBackButtonPressed()
        {
            // Сохраняем настройки перед выходом
            SaveSettings();

            // Возвращаемся на главную страницу
            return base.OnBackButtonPressed();
        }

        private async void OnResetClicked(object? sender, EventArgs e)
        {
            // Подтверждение сброса
            bool answer = await DisplayAlert(
                "Подтверждение",
                "Вы уверены, что хотите сбросить настройки к значениям по умолчанию?",
                "Да",
                "Нет");

            if (answer)
            {
                // Сбрасываем к значениям по умолчанию
                Preferences.Set("FilePath", DefaultFilePath);
                Preferences.Set("SearchText", DefaultSearchText);

                // Обновляем поля на экране
                FilePathEntry.Text = DefaultFilePath;
                SearchTextEntry.Text = DefaultSearchText;

                await DisplayAlert("Успех", "Настройки сброшены к значениям по умолчанию", "ОК");
            }
        }
    }
}
