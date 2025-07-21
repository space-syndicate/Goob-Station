using Robust.Shared.Configuration;
namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> NrpPanelEnabled =
        CVarDef.Create("nrp.panel_enabled", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> NrpMinutesBeforeBan =
        CVarDef.Create("nrp.minutes_before_ban", 10f, CVar.REPLICATED | CVar.SERVER);
}
