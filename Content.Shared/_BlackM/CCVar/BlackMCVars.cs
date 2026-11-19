using Robust.Shared.Configuration;

namespace Content.Shared._BlackM.CCVar;

[CVarDefs]
public sealed class BlackMCVars
{
    public static readonly CVarDef<string> LobbyBackgroundType =
        CVarDef.Create("blackm.lobby.background_type", "animation", CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyAnimation =
    CVarDef.Create("blackm.lobby.animation",
        "/Textures/_BlackM/LobbyScreens/lobbyscreen.rsi", CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyArt =
        CVarDef.Create("blackm.lobby.art", "random", CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyBackgroundPreset =
        CVarDef.Create("blackm.lobby.background_preset", "default", CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> EchoEnabled =
        CVarDef.Create("blackm.echo_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> EchoStrongPreset =
        CVarDef.Create("blackm.echo_strong_preset", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> DefaultAtmosphereShaderEnabled =
        CVarDef.Create("blackm.default_atmosphere_shader_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BattleMusicEnabled =
        CVarDef.Create("audio.battle_music_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BattleMusicVolume =
        CVarDef.Create("audio.battle_music_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);
    
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("blackm.barks_enabled", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> ReplaceTTSWithBarks =
        CVarDef.Create("blackm.replace_tts_with_barks", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("blackm.barks_volume", 2f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMinPitch =
        CVarDef.Create("blackm.barks_min_pitch", 0.5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> BarksMaxPitch =
        CVarDef.Create("blackm.barks_max_pitch", 2f, CVar.SERVER | CVar.REPLICATED);
    
    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("blackm.light_bloom_strength", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> MusicRadioVolume =
        CVarDef.Create("blackm.music_radio_volume", 1.0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> DiscordAuthEnabled =
        CVarDef.Create("discord_auth.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> DiscordAuthIsOptional =
        CVarDef.Create("discord_auth.is_optional", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> DiscordAuthInviteUrl =
        CVarDef.Create("discord_auth.invite_url", "https://discord.gg/8bvmPnkTPM", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> DiscordAuthLinkSecret =
        CVarDef.Create("discord_auth.link_secret", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordAuthLinkAllowedIps =
        CVarDef.Create("discord_auth.link_allowed_ips", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<bool> RadioSfxEnabled =
        CVarDef.Create("blackm.radio_sfx_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<float> RadioSfxVolume =
        CVarDef.Create("blackm.radio_sfx_volume", 1.0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> RadioDistortionChance =
        CVarDef.Create("blackm.radio_distortion_chance", 0.03f, CVar.SERVERONLY);

    public static readonly CVarDef<bool> RadioShowJobIcons =
        CVarDef.Create("blackm.radio_show_job_icons", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> RadioHideNames =
        CVarDef.Create("blackm.radio_hide_names", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> StartingLootVoteRequired =
        CVarDef.Create("blackm.startloot.vote_required", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> StartingLootVoteStartSeconds =
        CVarDef.Create("blackm.startloot.vote_start_seconds", 60, CVar.SERVERONLY);

    public static readonly CVarDef<int> StartingLootVoteDurationSeconds =
        CVarDef.Create("blackm.startloot.vote_duration_seconds", 30, CVar.SERVERONLY);

    public static readonly CVarDef<bool> GunCrosshairEnabled =
        CVarDef.Create("blackm.gun_crosshair", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> GunAimLineEnabled =
        CVarDef.Create("blackm.gun_aim_line", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> GunCameraFollow =
        CVarDef.Create("blackm.gun_camera_follow", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> GunCameraStrength =
        CVarDef.Create("blackm.gun_camera_strength", 0.6f, CVar.CLIENTONLY | CVar.ARCHIVE);
    
    public static readonly CVarDef<bool> ShotEffectsEnabled =
        CVarDef.Create("blackm.shot_effects_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ShotTrailsEnabled =
        CVarDef.Create("blackm.shot_trails_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ShotImpactsEnabled =
        CVarDef.Create("blackm.shot_impacts_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> VoidFallEnabled =
        CVarDef.Create("blackm.void_fall_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> VoidFallParallaxes =
        CVarDef.Create("blackm.void_fall_parallaxes", "BlackM", CVar.SERVERONLY);

    public static readonly CVarDef<bool> VoidFallLethal =
        CVarDef.Create("blackm.void_fall_lethal", false, CVar.SERVERONLY);

    public static readonly CVarDef<float> VoidFallDamage =
        CVarDef.Create("blackm.void_fall_damage", 10f, CVar.SERVERONLY);

    public static readonly CVarDef<float> VoidFallScreamChance =
        CVarDef.Create("blackm.void_fall_scream_chance", 0.5f, CVar.SERVERONLY);
}
