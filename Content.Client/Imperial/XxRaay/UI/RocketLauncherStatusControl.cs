using Content.Client.Weapons.Ranged.ItemStatus;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Imperial.XxRaay.UI;

public sealed class RocketLauncherStatusControl : Control
{
    private readonly BulletRender _bulletRender;

    public RocketLauncherStatusControl()
    {
        MinHeight = 15;
        HorizontalExpand = true;
        VerticalAlignment = VAlignment.Center;
        AddChild(_bulletRender = new BulletRender
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Bottom
        });
    }

    public void Update(int count, int capacity)
    {
        _bulletRender.Count = count;
        _bulletRender.Capacity = capacity;

        _bulletRender.Type = capacity switch
        {
            > 50 => BulletRender.BulletType.Tiny,
            > 15 => BulletRender.BulletType.Normal,
            _ => BulletRender.BulletType.Large
        };
    }
}
