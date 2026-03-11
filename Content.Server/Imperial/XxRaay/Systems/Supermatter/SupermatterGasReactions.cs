using System;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Radiation.Components;

namespace Content.Server.Imperial.XxRaay.Systems.Supermatter;

/// <summary>
/// Интерфейс реакции газа с суперматерией.
/// </summary>
public interface ISupermatterGasReaction
{
    void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime);
}

/// <summary>
/// Реакция термониума
/// </summary>
public sealed class ThermoniumIntegrityRegenReaction : ISupermatterGasReaction
{
    public void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime)
    {
        if (triggerGas != Gas.Thermonium)
            return;

        var moles = gas.GetMoles(triggerGas);
        if (moles <= gasComp.Comp.GasActivationMoles)
            return;

        var regen = gasComp.Comp.ThermoniumIntegrityRegenPerSecond * frameTime;
        integrity.Comp.Integrity = MathF.Min(integrity.Comp.MaxIntegrity, integrity.Comp.Integrity + regen);
    }
}

/// <summary>
/// Реакция, изменяющая интенсивность радиации
/// </summary>
public sealed class RadiationMultiplierReaction : ISupermatterGasReaction
{
    public void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime)
    {
        if (!system.TryGetComponent(integrity.Owner, out RadiationSourceComponent? radiation) || radiation == null)
            return;

        var baseIntensity = radiation.Intensity;

        if (system.TryGetComponent(integrity.Owner, out SupermatterEventComponent? eventComp) && eventComp != null)
        {
            if (eventComp.CurrentEvent != SupermatterEventComponent.SupermatterEventType.Radiation
                || eventComp.EventEndTime == TimeSpan.Zero)
            {
                baseIntensity = eventComp.DefaultRadiationIntensity;
            }
        }

        var multiplier = 1f;

        if (triggerGas is not (Gas.Ozonium or Gas.Plasma))
            return;

        if (gas.GetMoles(Gas.Ozonium) > gasComp.Comp.GasActivationMoles)
            multiplier *= gasComp.Comp.OzoneRadiationMultiplier;

        if (gas.GetMoles(Gas.Plasma) > gasComp.Comp.GasActivationMoles)
            multiplier *= gasComp.Comp.PlasmaRadiationMultiplier;

        radiation.Intensity = baseIntensity * multiplier;
    }
}

/// <summary>
/// Реакция антиноблия: выключает и включает суперматерию
/// </summary>
public sealed class AntiNobliumShutdownReaction : ISupermatterGasReaction
{
    public void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime)
    {
        if (triggerGas != Gas.AntiNoblium)
            return;

        if (!gasComp.Comp.AntiNobliumHardShutdownEnabled)
            return;

        var moles = gas.GetMoles(triggerGas);
        var active = moles > gasComp.Comp.GasActivationMoles;

        if (active)
        {
            integrity.Comp.Activated = false;
            gasComp.Comp.WasShutdownByAntiNoblium = true;
        }
        else if (!active && gasComp.Comp.WasShutdownByAntiNoblium)
        {
            integrity.Comp.Activated = true;
            gasComp.Comp.WasShutdownByAntiNoblium = false;
        }
    }
}

/// <summary>
/// Реакция трития: вычисляет множитель количества молний
/// </summary>
public sealed class TritiumLightningMultiplierReaction : ISupermatterGasReaction
{
    public void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime)
    {
        if (triggerGas != Gas.Tritium)
            return;

        var moles = gas.GetMoles(triggerGas);
        if (moles > gasComp.Comp.GasActivationMoles && gasComp.Comp.TritiumLightningMultiplier > 1f)
        {
            gasComp.Comp.CurrentLightningMultiplier = gasComp.Comp.TritiumLightningMultiplier;
        }
        else
        {
            gasComp.Comp.CurrentLightningMultiplier = 1f;
        }
    }
}

/// <summary>
/// Реакция гипер-ноблия: блокирует уничтожение существ
/// </summary>
public sealed class HyperNobliumTouchCancelReaction : ISupermatterGasReaction
{
    public void React(
        SupermatterGasSystem system,
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        Gas triggerGas,
        float frameTime)
    {
        if (triggerGas != Gas.HyperNoblium)
            return;

        var moles = gas.GetMoles(triggerGas);
        gasComp.Comp.HyperNobTouchCancelActive = moles > gasComp.Comp.GasActivationMoles;
    }
}

