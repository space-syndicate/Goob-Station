namespace Content.Shared.Imperial.Medical
{
    [RegisterComponent]
    public sealed partial class CustomHyposprayComponent : Component
    {
        [DataField("injectionDelayTime")]
        public TimeSpan InjectionDelayTime = TimeSpan.FromSeconds(1);
    }
}
