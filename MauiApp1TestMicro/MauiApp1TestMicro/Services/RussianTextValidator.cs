using System.Text.RegularExpressions;

namespace MauiApp1TestMicro.Services
{
    /// <summary>
    /// Класс для проверки и валидации распознанного русского текста
    /// </summary>
    public static class RussianTextValidator
    {
        private static readonly Regex CyrillicRegex = new Regex(@"[\u0400-\u04FF]");
        private static readonly Regex LatinRegex = new Regex(@"[a-zA-Z]");

        /// <summary>
        /// Проверяет, содержит ли текст кириллицу
        /// </summary>
        public static bool ContainsCyrillic(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return CyrillicRegex.IsMatch(text);
        }

        /// <summary>
        /// Проверяет, является ли текст преимущественно на кириллице
        /// </summary>
        public static bool IsPredominantlyCyrillic(string text, double threshold = 0.5)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            int cyrillicCount = 0;
            int latinCount = 0;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    if (CyrillicRegex.IsMatch(c.ToString()))
                        cyrillicCount++;
                    else if (LatinRegex.IsMatch(c.ToString()))
                        latinCount++;
                }
            }

            int totalLetters = cyrillicCount + latinCount;
            if (totalLetters == 0)
                return false;

            return (double)cyrillicCount / totalLetters >= threshold;
        }

        /// <summary>
        /// Очищает текст от лишних пробелов и спецсимволов
        /// </summary>
        public static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Удаляем множественные пробелы
            text = Regex.Replace(text, @"\s+", " ");
            // Удаляем пробелы в начале и конце
            text = text.Trim();

            return text;
        }
    }
}
