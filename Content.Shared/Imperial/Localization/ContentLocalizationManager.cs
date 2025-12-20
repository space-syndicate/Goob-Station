using System.Globalization;

namespace Content.Shared.Localizations;


public sealed partial class ContentLocalizationManager
{
    private void InitializeRussianLocale()
    {
        var culture = new CultureInfo("ru-RU");

        _loc.AddFunction(culture, "MAKEPLURAL", FormatMakePluralRu);
        _loc.AddFunction(culture, "MANY", FormatManyRu);
    }

    private ILocValue FormatMakePluralRu(LocArgs args)
    {
        var text = ((LocValueString)args.Args[0]).Value;
        var count = 2.0;

        if (args.Args.Count >= 2)
            count = ((LocValueNumber)args.Args[1]).Value;

        return new LocValueString(RussianPlura.DeclineWord(text, count));
    }

    private ILocValue FormatManyRu(LocArgs args)
    {
        var count = ((LocValueNumber)args.Args[1]).Value;

        if (Math.Abs(count - 1) < 0.0001f)
        {
            return (LocValueString)args.Args[0];
        }
        else
        {
            return (LocValueString)FormatMakePluralRu(args);
        }
    }
}
