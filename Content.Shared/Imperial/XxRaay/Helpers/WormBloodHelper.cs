namespace Content.Shared.Imperial.XxRaay.Helpers;

public static class WormBloodHelper
{
    public static short GetSeverity(int blood)
    {
        if (blood <= 19)
            return 0;

        if (blood > 300)
            return 16;

        return (short) (blood / 20);
    }
}
