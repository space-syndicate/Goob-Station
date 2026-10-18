// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Goobstation.Common.CCVar;

public sealed partial class GoobCVars
{
    /// <summary>Client preference for scanner silhouettes and the screen tint, from 0 to 1.</summary>
    public static readonly CVarDef<float> JanitorVisionIntensity =
        CVarDef.Create("accessibility.janitor_vision_intensity", 0.65f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
