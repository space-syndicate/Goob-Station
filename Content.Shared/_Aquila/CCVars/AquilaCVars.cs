// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Aquila.CCVars;

[CVarDefs]
public sealed class AquilaCVars
{
    /// <summary>
    ///     Список ID пресетов через запятую, при которых работают команды sotroundprepare и sotroundstart.
    ///     Пустая строка - команды работают при любом пресете.
    ///     Меняется на лету: cvar aquila.sot_allowed_presets "SoT,Extended"
    /// </summary>
    public static readonly CVarDef<string> SotAllowedPresets =
        CVarDef.Create("aquila.sot_allowed_presets", "SoT", CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> RadioMusicVolume =
        CVarDef.Create("aquila.radio_music_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
