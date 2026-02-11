using System.Text;

namespace SafonovMyBaby.Services
{
    public static class RussianTextValidator
    {
        private const double CyrillicThreshold = 0.3;

        public static bool IsPredominantlyCyrillic(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            int cyrillicCount = 0;
            int totalLetters = 0;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    totalLetters++;

                    // Проверяем на кириллицу ( диапазон Unicode)
                    if ((c >= '\u0400' && c <= '\u04FF') ||
                        (c >= '\u0500' && c <= '\u052F'))
                    {
                        cyrillicCount++;
                    }
                }
            }

            if (totalLetters == 0)
                return false;

            double ratio = (double)cyrillicCount / totalLetters;
            return ratio >= CyrillicThreshold;
        }

        public static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var cleaned = new StringBuilder();

            foreach (char c in text)
            {
                // Оставляем буквы, цифры, пробелы и базовую пунктуацию
                if (char.IsLetterOrDigit(c) ||
                    char.IsWhiteSpace(c) ||
                    c == ',' || c == '.' || c == '!' || c == '?' ||
                    c == ':' || c == ';' || c == '-' ||
                    c == '(' || c == ')' || c == '"' || c == '\'')
                {
                    cleaned.Append(c);
                }
            }

            return cleaned.ToString();
        }
    }
}
