namespace Content.Shared.Imperial.Helpers;

/// <summary>
/// Вспомогательный класс для работы с фракциями
/// </summary>
public static class FactionHelper
{
    public enum FactionType
    {
        Neutral,
        Green,
        Yellow,
        Red,
        Blue,
        NTFaction,
        USSP,
        Sindi
    }

    public static FactionType GetFlagFactionEnum(string prototypeId)
    {
        return prototypeId switch
        {
            "ImperialGreenFlag" => FactionType.Green,
            "ImperialYellowFlag" => FactionType.Yellow,
            "ImperialRedFlag" => FactionType.Red,
            "ImperialBlueFlag" => FactionType.Blue,
            "ImperialNTFlag" => FactionType.NTFaction,
            "ImperialUSSPFlag" => FactionType.USSP,
            "ImperialSindiFlag" => FactionType.Sindi,
            "ImperialWhiteFlag" => FactionType.Neutral,
            _ => FactionType.Neutral
        };
    }

    public static string GetFlagFaction(string prototypeId)
    {
        return GetFlagFactionEnum(prototypeId) switch
        {
            FactionType.Green => "GreenFaction",
            FactionType.Yellow => "YellowFaction",
            FactionType.Red => "RedFaction",
            FactionType.Blue => "BlueFaction",
            FactionType.NTFaction => "NTFaction",
            FactionType.USSP => "USSPFaction",
            FactionType.Sindi => "SindiFaction",
            _ => "NeutralFaction"
        };
    }

    public static string GetFactionFlagPrototype(string faction)
    {
        return faction switch
        {
            "GreenFaction" => "ImperialGreenFlag",
            "YellowFaction" => "ImperialYellowFlag",
            "RedFaction" => "ImperialRedFlag",
            "BlueFaction" => "ImperialBlueFlag",
            "NTFaction" => "ImperialNTFlag",
            "USSPFaction" => "ImperialUSSPFlag",
            "SindiFaction" => "ImperialSindiFlag",
            _ => "ImperialWhiteFlag"
        };
    }

    public static string GetFactionFlagPrototype(FactionType faction)
    {
        return faction switch
        {
            FactionType.Green => "ImperialGreenFlag",
            FactionType.Yellow => "ImperialYellowFlag",
            FactionType.Red => "ImperialRedFlag",
            FactionType.Blue => "ImperialBlueFlag",
            FactionType.NTFaction => "ImperialNTFlag",
            FactionType.USSP => "ImperialUSSPFlag",
            FactionType.Sindi => "ImperialSindiFlag",
            _ => "ImperialWhiteFlag"
        };
    }

    public static string? GetCurrencyIdForFaction(string faction)
    {
        return faction switch
        {
            "NTFaction" => "NTFactionPoints",
            "SindiFaction" => "SindiFactionPoints",
            "GreenFaction" => "GreenFactionPoints",
            "YellowFaction" => "YellowFactionPoints",
            "RedFaction" => "RedFactionPoints",
            "BlueFaction" => "BlueFactionPoints",
            "USSPFaction" => "USSPFactionPoints",
            _ => null
        };
    }

    public static string? GetCurrencyIdForFaction(FactionType faction)
    {
        return faction switch
        {
            FactionType.NTFaction => "NTFactionPoints",
            FactionType.Sindi => "SindiFactionPoints",
            FactionType.Green => "GreenFactionPoints",
            FactionType.Yellow => "YellowFactionPoints",
            FactionType.Red => "RedFactionPoints",
            FactionType.Blue => "BlueFactionPoints",
            FactionType.USSP => "USSPFactionPoints",
            _ => null
        };
    }

    public static string MapFactionFromComponent(string factionComponentValue)
    {
        return factionComponentValue switch
        {
            "Nanotrasen" => "NTFaction",
            "Syndicate" => "SindiFaction",
            "GreenFaction" => "GreenFaction",
            "YellowFaction" => "YellowFaction",
            "RedFaction" => "RedFaction",
            "BlueFaction" => "BlueFaction",
            "USSPFaction" => "USSPFaction",
            _ => factionComponentValue
        };
    }

    public static FactionType MapFactionFromComponentEnum(string factionComponentValue)
    {
        return factionComponentValue switch
        {
            "Nanotrasen" => FactionType.NTFaction,
            "Syndicate" => FactionType.Sindi,
            "GreenFaction" => FactionType.Green,
            "YellowFaction" => FactionType.Yellow,
            "RedFaction" => FactionType.Red,
            "BlueFaction" => FactionType.Blue,
            "USSPFaction" => FactionType.USSP,
            _ => FactionType.Neutral
        };
    }
}

