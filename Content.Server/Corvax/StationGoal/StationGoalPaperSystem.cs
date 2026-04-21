using System.Linq;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Server.Imperial.StationGoal;
using Content.Shared.Paper;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Corvax.StationGoal
{
    //<summary>
    //    System to spawn paper with station goal.
    //</summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            SendRandomGoal();
        }

        public bool SendRandomGoal()
        {
            var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>().ToList();
            var goal = _random.Pick(availableGoals);
            return SendStationGoal(goal);
        }

        //<summary>
        //    Send a station goal to all faxes which are authorized to receive it.
        //</summary>
        //<returns>True if at least one fax received paper</returns>
        public bool SendStationGoal(StationGoalPrototype goal)
        {
            var enumerator = EntityQueryEnumerator<FaxMachineComponent>();
            var wasSent = false;
            var funny = new StampDisplayInfo() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") };
            var list = new List<StampDisplayInfo>();
            list.Add(funny);

            while (enumerator.MoveNext(out var fax, out var faxComponent))
            {
                if (!faxComponent.ReceiveStationGoal) continue;

                var printout = new FaxPrintout(
                    Loc.GetString(goal.Text),
                    Loc.GetString("station-goal-fax-paper-name"),
                    null,
                    null,
                    "paper_stamp-centcom",
                    list
                );
                _faxSystem.Receive(fax, printout, null, faxComponent);

                wasSent = true;
            }

            return wasSent;
        }
    }
}
