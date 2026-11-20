using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._BlackM.ChatImage;

[CVarDefs]
public sealed class BlackMCVars : CVars
{
    public static readonly CVarDef<bool> ChatImageEnabled =
        CVarDef.Create("blackm.chatimage.enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> ChatImageHosts =
        CVarDef.Create(
            "blackm.chatimage.hosts",
            "i.imgur.com,cdn.discordapp.com,media.discordapp.net,i.postimg.cc,files.catbox.moe,media.tenor.com,c.tenor.com,media.giphy.com,i.giphy.com",
            CVar.SERVERONLY);

    public static readonly CVarDef<int> ChatImageMaxKb =
        CVarDef.Create("blackm.chatimage.max_kb", 4096, CVar.SERVERONLY);
}
