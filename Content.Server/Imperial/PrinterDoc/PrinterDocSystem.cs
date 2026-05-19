using System.Text.RegularExpressions;
using Content.Server.Station.Systems;
using Content.Server.Administration.Logs;
using Content.Shared.PrinterDoc;
using Content.Server.Access.Systems;
using Content.Server.Lathe;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Paper;

namespace Content.Server.PrinterDoc
{
    public sealed class PrinterDocSystem : EntitySystem
    {
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly SharedMaterialStorageSystem _materialStorageSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<PrinterDocCheckIdCardMessage>(OnPrinterDocCheckIdCard);
        }

        private void OnPrinterDocCheckIdCard(PrinterDocCheckIdCardMessage msg, EntitySessionEventArgs args)
        {
            var uid = args.SenderSession.AttachedEntity;
            if (uid == null || !TryComp<LatheComponent>(uid, out var latheComponent))
                return;

            latheComponent.UseCardId = msg.UseCardId;
            EntityManager.System<LatheSystem>().UpdateUserInterfaceState(uid.Value, latheComponent);
        }

        public void TrySetContentPrintedDocument(EntityUid uid, EntityUid? userId, bool useCardId)
        {
            if (!TryComp<PaperComponent>(uid, out var paperComp))
                return;

            var content = paperComp.Content;
            var stationName = GetStationNameForObject(uid);
            var currentDate = DateTime.Now.AddYears(1000).ToString("dd/MM/yyyy").Replace(".", "/");

            if (stationName.Length > 5)
            {
                content = Loc.GetString(content)
                    .Replace("NT14-XX-###", "NT14-" + stationName.Substring(stationName.Length - 6))
                    .Replace("ДД/ММ/3024", currentDate);
            }
            else
            {
                content = Loc.GetString(content)
                    .Replace("NT14-XX-###", "NT14-" + stationName)
                    .Replace("ДД/ММ/3024", currentDate);
            }

            if (useCardId && userId != null)
            {
                if (_idCardSystem.TryFindIdCard(userId.Value, out var idCard))
                {
                    if (idCard.Comp != null)
                    {
                        var profession = string.Empty;
                        var idCardEntity = idCard.Owner;
                        var metaData = MetaData(idCardEntity);

                        var entityName = metaData.EntityName;

                        var regex = new Regex(@"\((.*?)\)");
                        var match = regex.Match(entityName);

                        if (match.Success && match.Groups.Count > 1)
                        {
                            profession = match.Groups[1].Value;
                        }

                        content = content
                            .Replace("ПОДОТЧЁТНОЕ ЛИЦО:", "ПОДОТЧЁТНОЕ ЛИЦО: " + (idCard.Comp.FullName ?? string.Empty))
                            .Replace("ДОЛЖНОСТЬ:", "ДОЛЖНОСТЬ: " + profession);
                    }
                }
            }

            if (!TryComp<PaperComponent>(uid, out var paperComponent))
                return;

            var paperEntity = new Entity<PaperComponent>(uid, paperComponent);

            _paper.SetContent(paperEntity, content);
        }

        private string GetStationNameForObject(EntityUid uid)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);

            if (stationUid == null)
                return string.Empty;

            return MetaData(stationUid.Value).EntityName;
        }

        public bool TryToCheckPrinter(EntityUid uid)
        {
            if (_entityManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                var prototypeId = metaData.EntityPrototype?.ID;
                if (prototypeId != null && _prototypeManager.TryIndex<EntityPrototype>(prototypeId, out var prototype))
                {
                    if (prototype.ID == "PrinterDoc")
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        public void TryAddPaperToPrinter(EntityUid toInsert, MaterialStorageComponent? storage, EntityUid receiver, SharedPopupSystem popup, IAdminLogManager adminLogger, EntityUid user)
        {
            if (HasComp<PaperComponent>(toInsert) && TryToCheckPrinter(receiver))
            {
                _materialStorageSystem.TryChangeMaterialAmount(receiver, "Paper", 100, storage);

                popup.PopupEntity(Loc.GetString("machine-insert-item", ("user", user), ("machine", receiver), ("item", toInsert)), receiver);
                QueueDel(toInsert);

                adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):player} inserted paper into {ToPrettyString(receiver):receiver}");
            }
        }
    }
}
