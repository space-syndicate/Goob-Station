using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using Content.Client.Message;
using Content.Shared.Imperial.ImperialBorgs;

namespace Content.Client.Borgs
{
    public sealed class BorgHypoStatusControl : Control
    {
        private readonly BorgHypoComponent _parent;
        private readonly RichTextLabel _label;

        public BorgHypoStatusControl(BorgHypoComponent parent)
        {
            _parent = parent;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            _label.SetMarkup(parent.CurrentReagentName);
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_parent.UiUpdateNeeded)
            {
                _parent.UiUpdateNeeded = false;
                Update();
            }
        }

        public void Update()
        {
            _label.SetMarkup(_parent.CurrentReagentName);
        }
    }
}
