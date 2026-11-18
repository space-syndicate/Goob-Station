// SPDX-FileCopyrightText: 2022 Alex Evgrashin <aevgrashin@yandex.ru>
// SPDX-FileCopyrightText: 2022 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2023 Slava0135 <40753025+Slava0135@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Bloodtrak;
using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client.Bloodtrak;

public sealed class ClientBloodtrakSystem : SharedBloodtrakSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void FrameUpdate(float frameTime) // _BlackM: moved from Update, visuals once per frame
    {
        base.FrameUpdate(frameTime);

        var eyeRotation = _eyeManager.CurrentEye.Rotation; // _BlackM: hoisted out of the loop
        var query = EntityQueryEnumerator<BloodtrakComponent, SpriteComponent>();
        while (query.MoveNext(out var pinpointer, out var sprite))
        {
            if (!pinpointer.HasTarget)
                continue;

            var angle = pinpointer.ArrowAngle + eyeRotation; // _BlackM: cached eye rotation

            switch (pinpointer.DistanceToTarget)
            {
                case Shared.Bloodtrak.Distance.Close:
                case Shared.Bloodtrak.Distance.Medium:
                case Shared.Bloodtrak.Distance.Far:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                default:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
