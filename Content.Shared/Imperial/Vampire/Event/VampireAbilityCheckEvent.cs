namespace Content.Shared.Imperial.Vampire
{
    /// <summary>
    /// ивент для проверки и выдачи новых способностей
    /// вызывается после увеличения TotalDrunk для безопасной выдачи способностей вне DoAfter
    /// </summary>
    public sealed class VampireAbilityCheckEvent : EntityEventArgs
    {
        public EntityUid Uid { get; }
        public int SelectedSubgroup { get; }

        public VampireAbilityCheckEvent(EntityUid uid, int selectedSubgroup)
        {
            Uid = uid;
            SelectedSubgroup = selectedSubgroup;
        }
    }
}
