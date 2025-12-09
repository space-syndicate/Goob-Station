namespace Content.Shared.Imperial.Medical
{
    [RegisterComponent]
    public sealed partial class CustomHyposprayComponent : Component
    {
        public TimeSpan InjectionDelayTime = TimeSpan.FromSeconds(1);
    }
}
