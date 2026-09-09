// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Components;
using Content.Shared.Ghost;
using Content.Shared.Overlays;
using Robust.Shared.Utility;

namespace Content.Server.Ghost
{
    public sealed partial class GhostSystem
    {
        private const string GhostObserverHudActionDescOff = "ghost-gui-observer-hud-action-desc-off";
        private const string GhostObserverHudActionDescSecurity = "ghost-gui-observer-hud-action-desc-security";
        private const string GhostObserverHudActionDescSecurityHealth = "ghost-gui-observer-hud-action-desc-security-health";

        private static readonly SpriteSpecifier.Rsi GhostObserverHudSecurityIcon =
            new(new ResPath("_CorvaxGoob/Interface/Actions/ghost_sec_hud.rsi"), "red_eye_32");

        private static readonly SpriteSpecifier.Rsi GhostObserverHudSecurityHealthIcon =
            new(new ResPath("_CorvaxGoob/Interface/Actions/ghost_sec_hud.rsi"), "red_blue_eye_32");

        private void InitializeGhostObserverHud()
        {
            SubscribeLocalEvent<GhostComponent, ToggleGhostObserverHudActionEvent>(OnGhostObserverHudAction);
        }

        private void AddGhostObserverHudAction(EntityUid uid, GhostComponent component)
        {
            // Only regular ghosts need this HUD action; admin ghosts already have admin overlays.
            if (component.CanGhostInteract)
                return;

            var hud = EnsureComp<GhostObserverHudComponent>(uid);
            // Start each new observer at the beginning of the action cycle.
            hud.Mode = GhostObserverHudMode.Off;

            // AddAction creates the action entity, so update its initial icon and tooltip only after it succeeds.
            if (_actions.AddAction(uid, ref component.ToggleGhostObserverHudActionEntity, component.ToggleGhostObserverHudAction))
                SetGhostObserverHudActionState(component, false, GhostObserverHudSecurityIcon, GhostObserverHudActionDescOff);
        }

        private void ShutdownGhostObserverHud(EntityUid uid, GhostComponent component)
        {
            DisableGhostObserverHud(uid);
            _actions.RemoveAction(uid, component.ToggleGhostObserverHudActionEntity);
            RemComp<GhostObserverHudComponent>(uid);
        }

        private void OnGhostObserverHudAction(EntityUid uid, GhostComponent component, ToggleGhostObserverHudActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            // The mode component is the source of truth; overlay components are only the applied result.
            var hud = EnsureComp<GhostObserverHudComponent>(uid);
            hud.Mode = GetNextGhostObserverHudMode(hud.Mode);

            ApplyGhostObserverHudMode(uid, component, hud.Mode);
        }

        private static GhostObserverHudMode GetNextGhostObserverHudMode(GhostObserverHudMode mode)
        {
            return mode switch
            {
                GhostObserverHudMode.Off => GhostObserverHudMode.Security,
                GhostObserverHudMode.Security => GhostObserverHudMode.SecurityHealth,
                _ => GhostObserverHudMode.Off,
            };
        }

        private void ApplyGhostObserverHudMode(EntityUid uid, GhostComponent component, GhostObserverHudMode mode)
        {
            switch (mode)
            {
                case GhostObserverHudMode.Security:
                    EnableGhostObserverSecurityHud(uid);
                    DisableGhostObserverHealthHud(uid);
                    SetGhostObserverHudActionState(component, true, GhostObserverHudSecurityIcon, GhostObserverHudActionDescSecurity);
                    Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-observer-hud-popup-security"), uid, uid);
                    break;
                case GhostObserverHudMode.SecurityHealth:
                    EnableGhostObserverSecurityHud(uid);
                    EnableGhostObserverHealthHud(uid);
                    SetGhostObserverHudActionState(component, true, GhostObserverHudSecurityHealthIcon, GhostObserverHudActionDescSecurityHealth);
                    Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-observer-hud-popup-security-health"), uid, uid);
                    break;
                case GhostObserverHudMode.Off:
                    DisableGhostObserverHud(uid);
                    SetGhostObserverHudActionState(component, false, GhostObserverHudSecurityIcon, GhostObserverHudActionDescOff);
                    Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-observer-hud-popup-off"), uid, uid);
                    break;
            }
        }

        private void EnableGhostObserverSecurityHud(EntityUid uid)
        {
            // These HUD components only display existing security status icons.
            // They do not grant the ghost any status-editing interactions.
            EnsureComp<ShowJobIconsComponent>(uid);
            EnsureComp<ShowMindShieldIconsComponent>(uid);
            EnsureComp<ShowCriminalRecordIconsComponent>(uid);
            EnsureComp<ShowSquadIconsComponent>(uid);
        }

        private void DisableGhostObserverSecurityHud(EntityUid uid)
        {
            RemComp<ShowJobIconsComponent>(uid);
            RemComp<ShowMindShieldIconsComponent>(uid);
            RemComp<ShowCriminalRecordIconsComponent>(uid);
            RemComp<ShowSquadIconsComponent>(uid);
        }

        private void EnableGhostObserverHealthHud(EntityUid uid)
        {
            var healthBars = EnsureComp<ShowHealthBarsComponent>(uid);
            // Biological covers living mobs, while Silicon covers borg chassis.
            healthBars.DamageContainers.Clear();
            healthBars.DamageContainers.Add("Biological");
            healthBars.DamageContainers.Add("Silicon");
            Dirty(uid, healthBars);
        }

        private void DisableGhostObserverHealthHud(EntityUid uid)
        {
            RemComp<ShowHealthBarsComponent>(uid);
        }

        private void DisableGhostObserverHud(EntityUid uid)
        {
            DisableGhostObserverSecurityHud(uid);
            DisableGhostObserverHealthHud(uid);
        }

        private void SetGhostObserverHudActionState(
            GhostComponent component,
            bool toggled,
            SpriteSpecifier? iconOn,
            string descriptionLocId)
        {
            if (component.ToggleGhostObserverHudActionEntity is not { } action)
                return;

            _actions.SetIconOn(action, iconOn);
            _actions.SetToggled(action, toggled);
            // Action tooltips read metadata directly, so store the resolved text instead of the locale id.
            _metaData.SetEntityDescription(action, Loc.GetString(descriptionLocId));
        }
    }
}
