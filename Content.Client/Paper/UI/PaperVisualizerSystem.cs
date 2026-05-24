using Robust.Client.GameObjects;

using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

public sealed class PaperVisualizerSystem : VisualizerSystem<PaperVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PaperVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<PaperStatus>(uid, PaperVisuals.Status, out var writingStatus, args.Component))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);

        if (AppearanceSystem.TryGetData<string>(uid, PaperVisuals.Stamp, out var stampState, args.Component))
        {
            // imperial space: fix bug. start
            if (PaperVisualsComponent.BlackListRsi.Contains(args.Sprite.BaseRSI?.Path.ToString()!))
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, false);
                return;
            }
            // imperial space: fix bug. end

            if (stampState != string.Empty)
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PaperVisualLayers.Stamp, stampState);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, true);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, false);
            }

        }
    }
}

public enum PaperVisualLayers
{
    Stamp,
    Writing
}
