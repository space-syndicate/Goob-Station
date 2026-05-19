using System.Linq;

namespace Content.Shared.Localizations;


/// <summary>
/// Этот класс - нейрослоп
/// Честно, я не хочу в это лезть. Хоть ИИ я и не люблю использовать, но тут я заебусь это все делать
/// <para>
/// И чувствую, что я об этом пожалею. Ненавижу, блять, ИИ
/// </para>
/// </summary>
public sealed class RussianPlura
{
    public static string DeclineWord(string phrase, double number)
    {
        if (string.IsNullOrEmpty(phrase))
            return phrase;

        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Все слова склоняем в родительный падеж
        var declinedWords = words.Select(word => GetGenitiveSingularNoun(word)).ToArray();

        return $"{string.Join(" ", declinedWords)}".Trim();
    }

    private static string GetGenitiveSingularNoun(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2) return word;

        var lastChar = word[^1];
        var secondLastChar = word.Length > 1 ? word[^2] : '\0';
        var lowerWord = word.ToLower();

        // Вещества и материалы (особые случаи)
        if (lowerWord == "плазма" || lowerWord == "плазм") return "плазмы";
        if (lowerWord == "уран" || lowerWord == "уранов") return "урана";
        if (lowerWord == "сталь" || lowerWord == "сталей") return "стали";
        if (lowerWord == "дерево" || lowerWord == "деревьев") return "дерева";
        if (lowerWord == "пластик" || lowerWord == "пластиков") return "пластика";
        if (lowerWord == "металл" || lowerWord == "металлов") return "металла";
        if (lowerWord == "стекло" || lowerWord == "стекол") return "стекла";

        // Слова на -я (сталь -> стали)
        if (lastChar == 'ь')
        {
            return word[..^1] + "и"; // сталь -> стали
        }

        // Слова на -ка (доска -> доски)
        if (lowerWord.EndsWith("ка"))
        {
            return word[..^1] + "и"; // доска -> доски
        }

        // Слова на -ль (сталь -> стали)
        if (lowerWord.EndsWith("ль"))
        {
            return word[..^1] + "и"; // сталь -> стали
        }

        return (lastChar, secondLastChar) switch
        {
            ('к', 'и') => word[..^1] + "ка",    // пластик -> пластика
            ('к', 'о') => word[..^2] + "ка",    // листок -> листка
            ('ц', 'е') => word[..^2] + "ца",    // дворец -> дворца
            ('ь', _) => word[..^1] + "и",       // тетрадь -> тетради
            ('а', _) => word[..^1] + "ы",       // книга -> книги
            ('я', _) => word[..^1] + "и",       // статья -> статьи
            ('о', _) => word[..^1] + "а",       // окно -> окна
            ('е', _) => word[..^1] + "я",       // поле -> поля
            ('й', _) => word[..^1] + "я",       // музей -> музея
            ('ы', _) => word[..^1] + "",        // доски -> досок
            _ when IsConsonant(lastChar) => word + "а", // стол -> стола
            _ => word
        };
    }

    private static bool IsVowel(char c)
    {
        var lowerC = char.ToLower(c);
        return lowerC == 'а' || lowerC == 'е' || lowerC == 'ё' || lowerC == 'и' ||
               lowerC == 'о' || lowerC == 'у' || lowerC == 'ы' || lowerC == 'э' ||
               lowerC == 'ю' || lowerC == 'я';
    }

    private static bool IsConsonant(char c)
    {
        var lowerC = char.ToLower(c);
        return !IsVowel(lowerC) && char.IsLetter(lowerC);
    }
}
