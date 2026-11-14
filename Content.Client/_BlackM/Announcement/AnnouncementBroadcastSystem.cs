using Content.Client._BlackM.WelcomeMessage;
using Content.Client.Resources;
using Content.Shared._BlackM.Announcement;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._BlackM.Announcement;

public sealed class AnnouncementBroadcastSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly WelcomeMessageSystem _welcome = default!;

    private const string FallbackPortrait = "/Textures/_BlackM/Interface/welcome_portrait.png";

    private const float EntityWaitSeconds = 1.5f;
    private const float GapSeconds = 0.4f;
    private const int MaxQueue = 6;

    private readonly Queue<AnnouncementBroadcastEvent> _queue = new();
    private AnnouncementBroadcastEvent? _pending;
    private float _pendingWait;
    private float _gap;
    private WelcomeMessageControl? _current;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AnnouncementBroadcastEvent>(OnMessage);
    }

    private void OnMessage(AnnouncementBroadcastEvent ev)
    {
        while (_queue.Count >= MaxQueue)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(ev);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_current != null)
        {
            if (!_current.Finished)
                return;

            _current.Orphan();
            _current = null;
            _gap = GapSeconds;
        }

        if (_gap > 0f)
        {
            _gap -= frameTime;
            return;
        }

        if (_welcome.IsShowing)
            return;

        if (_pending == null)
        {
            if (!_queue.TryDequeue(out var next))
                return;

            _pending = next;
            _pendingWait = 0f;
        }

        _pendingWait += frameTime;

        EntityUid? speaker = null;
        if (_pending.Speaker is { } netSpeaker &&
            TryGetEntity(netSpeaker, out var uid) &&
            HasComp<SpriteComponent>(uid.Value))
        {
            speaker = uid.Value;
        }

        if (speaker == null && _pendingWait < EntityWaitSeconds)
            return;

        Show(_pending, speaker);
        _pending = null;
    }

    private void Show(AnnouncementBroadcastEvent ev, EntityUid? speaker)
    {
        var control = new WelcomeMessageControl(
            ev.Sender,
            ev.Title,
            ev.Text,
            speaker == null ? _res.GetTexture(FallbackPortrait) : null,
            ev.Accent,
            ev.Duration,
            _welcome.GetFonts(),
            speaker);

        control.PlaySound += _welcome.PlayWelcomeSound;

        var parent = _welcome.GetParent();
        parent.AddChild(control);

        if (parent is LayoutContainer)
        {
            LayoutContainer.SetAnchorPreset(control, LayoutContainer.LayoutPreset.CenterTop);
            LayoutContainer.SetGrowHorizontal(control, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetMarginLeft(control, 0);
            LayoutContainer.SetMarginRight(control, 0);
            LayoutContainer.SetMarginTop(control, WelcomeMessageControl.TopMargin);
            LayoutContainer.SetMarginBottom(control, WelcomeMessageControl.TopMargin);
        }

        _current = control;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _current?.Orphan();
        _current = null;
        _pending = null;
        _queue.Clear();
    }
}
