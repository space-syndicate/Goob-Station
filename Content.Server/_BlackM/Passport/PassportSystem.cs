using Content.Server._BlackM.Achievements;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._BlackM.Passport;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._BlackM.Passport;

public sealed class PassportSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem  _ui     = default!;
    [Dependency] private readonly HandsSystem          _hands  = default!;
    [Dependency] private readonly InventorySystem      _inv    = default!;
    [Dependency] private readonly ILocalizationManager _loc    = default!;
    [Dependency] private readonly IPrototypeManager    _proto  = default!;
    [Dependency] private readonly IRobustRandom        _random = default!;
    [Dependency] private readonly PopupSystem          _popup  = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly AchievementsSystem _achievements = default!;

    private const float BureaucraticErrorChance = 0.10f;

    private static readonly HashSet<string> ImmuneJobIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "Captain",
    };

    private static readonly string[] CityKeys =
    {
        "passport-city-1",
        "passport-city-2",
        "passport-city-3",
        "passport-city-4",
    };

    private static readonly string[] CityErrorKeys =
    {
        "passport-city-error-1",
        "passport-city-error-2",
        "passport-city-error-3",
        "passport-city-error-4",
        "passport-city-error-5",
        "passport-city-error-6",
        "passport-city-error-7",
        "passport-city-error-8",
        "passport-city-error-9",
        "passport-city-error-10",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PassportComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PassportComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PassportComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PassportComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PassportComponent, ComponentShutdown>(OnPassportShutdown);
    }

    private void OnUiOpened(EntityUid uid, PassportComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, comp);
    }

    private void OnActivate(EntityUid uid, PassportComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled) return;
        UpdateUiState(uid, comp);
        _ui.TryToggleUi(uid, PassportUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnUseInHand(EntityUid uid, PassportComponent comp, UseInHandEvent args)
    {
        if (args.Handled) return;
        UpdateUiState(uid, comp);
        _ui.TryToggleUi(uid, PassportUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnExamined(EntityUid uid, PassportComponent comp, ExaminedEvent args)
    {
        if (!string.IsNullOrEmpty(comp.AdditionalJob))
            args.PushMarkup(Loc.GetString("passport-examine-additional-job", ("job", comp.AdditionalJob)));

        if (!comp.Checked)
            return;

        args.PushMarkup(Loc.GetString("passport-examine-checked-by", ("name", comp.CheckedBy)));
    }
    public bool TrySetAdditionalJob(EntityUid passportUid, string positionId, string displayName,
        PassportComponent? comp = null)
    {
        if (!Resolve(passportUid, ref comp))
            return false;

        if (!string.IsNullOrEmpty(comp.AdditionalJobId))
            return false;

        comp.AdditionalJobId = positionId;
        comp.AdditionalJob   = displayName;
        Dirty(passportUid, comp);
        UpdateUiState(passportUid, comp);
        return true;
    }

    private EntityUid? CreatePreviewDoll(EntityUid characterUid)
    {
        if (!TryComp<HumanoidAppearanceComponent>(characterUid, out var appearance))
            return null;

        if (!_proto.TryIndex<SpeciesPrototype>(appearance.Species, out var species))
            return null;

        var doll = Spawn(species.DollPrototype, MapCoordinates.Nullspace);
        _humanoid.CloneAppearance(characterUid, doll);
        CopyClothing(characterUid, doll);

        _pvsOverride.AddGlobalOverride(doll);

        return doll;
    }

    private void CopyClothing(EntityUid source, EntityUid target)
    {
        if (!_inv.TryGetSlots(source, out var slots))
            return;

        foreach (var slot in slots)
        {
            if (!_inv.TryGetSlotEntity(source, slot.Name, out var itemUid))
                continue;

            if (HasComp<PassportComponent>(itemUid))
                continue;

            var protoId = MetaData(itemUid.Value).EntityPrototype?.ID;
            if (protoId == null)
                continue;

            var copy = Spawn(protoId, MapCoordinates.Nullspace);
            _inv.TryEquip(target, copy, slot.Name, force: true, silent: true);
        }
    }

    private void OnPassportShutdown(EntityUid uid, PassportComponent comp, ComponentShutdown args)
    {
        if (comp.OwnerEntity is { } netEnt && TryGetEntity(netEnt, out var dollUid) && Exists(dollUid))
        {
            _pvsOverride.RemoveGlobalOverride(dollUid.Value);
            QueueDel(dollUid.Value);
        }
    }

    private void UpdateUiState(EntityUid uid, PassportComponent comp)
    {
        _ui.SetUiState(uid, PassportUiKey.Key, BuildState(comp));
    }
    public PassportBoundUserInterfaceState BuildState(PassportComponent comp)
    {
        var displayCity    = comp.City;
        var displayNumber  = comp.PassportNumber;
        var displaySurname = comp.Surname;
        var displayName    = comp.OwnerName;
        var displayJob     = comp.JobTitle;
        var displayDate    = comp.IssuedDate;

        if (comp.HasBureaucraticError && !string.IsNullOrEmpty(comp.ErrorField))
        {
            switch (comp.ErrorField)
            {
                case "city":           displayCity    = comp.ErrorValue; break;
                case "passportnumber": displayNumber  = comp.ErrorValue; break;
                case "surname":        displaySurname = comp.ErrorValue; break;
                case "name":           displayName    = comp.ErrorValue; break;
                case "job":            displayJob     = comp.ErrorValue; break;
                case "issueddate":     displayDate    = comp.ErrorValue; break;
            }
        }

        var signature = BuildSignature(displaySurname, displayName);
        var mrz1      = BuildMrz1(displayNumber);
        var mrz2      = BuildMrz2(displaySurname, displayName);

        return new PassportBoundUserInterfaceState(
            displayName, displaySurname, displayCity, displayJob,
            displayNumber, displayDate,
            signature, mrz1, mrz2, comp.OwnerEntity,
            comp.HasBureaucraticError, comp.ErrorField, comp.Stamp,
            comp.AdditionalJob, new List<string>(comp.Badges));
    }
    public void RefreshBadges(EntityUid mob)
    {
        if (!_inv.TryGetSlotEntity(mob, "passport", out var slot) || slot == null)
            return;

        if (!TryComp<PassportComponent>(slot.Value, out var comp) || !comp.IsBound)
            return;

        comp.Badges = _achievements.GetBadgesFor(mob);
        Dirty(slot.Value, comp);
        UpdateUiState(slot.Value, comp);
    }

    public void SetStamp(EntityUid passportUid, PassportStampState stamp, PassportComponent? comp = null)
    {
        if (!Resolve(passportUid, ref comp)) return;

        comp.Stamp = stamp;
        Dirty(passportUid, comp);
        UpdateUiState(passportUid, comp);
    }

    public void FillPassport(EntityUid passportUid, EntityUid characterUid,
        string firstName, string surname, string jobTitle, string cityKey,
        PassportComponent? comp = null, string jobId = "")
    {
        if (!Resolve(passportUid, ref comp)) return;
        if (comp.IsBound) return;

        comp.OwnerName      = firstName;
        comp.Surname        = string.IsNullOrWhiteSpace(surname)
            ? _loc.GetString("passport-surname-absent")
            : surname;
        comp.JobTitle       = jobTitle;
        comp.City           = _loc.GetString(cityKey);
        comp.OwnerEntity    = GetNetEntity(CreatePreviewDoll(characterUid) ?? characterUid);
        comp.PassportNumber = GenerateNumber();
        comp.IssuedDate     = DateTime.UtcNow.ToString("dd.MM.yyyy");
        comp.IsBound        = true;
        comp.Stamp          = PassportStampState.None;
        comp.Badges         = _achievements.GetBadgesFor(characterUid);

        if (!IsImmuneJobId(jobId))
            TryApplyBureaucraticError(comp);

        Dirty(passportUid, comp);
    }

    public EntityUid SpawnAndGivePassport(EntityUid characterUid,
        string firstName, string surname, string jobTitle,
        string cityKey = "passport-city-1", string jobId = "")
    {
        var passport = Spawn("PassportBM", Transform(characterUid).Coordinates);
        FillPassport(passport, characterUid, firstName, surname, jobTitle, cityKey, jobId: jobId);

        if (_inv.TryEquip(characterUid, passport, "passport", force: true))
            return passport;

        _hands.TryPickupAnyHand(characterUid, passport);
        return passport;
    }

    public void ForceApplyBureaucraticError(EntityUid passportUid, PassportComponent? comp = null)
    {
        if (!Resolve(passportUid, ref comp)) return;
        ApplyBureaucraticError(comp);
        Dirty(passportUid, comp);
        UpdateUiState(passportUid, comp);
    }

    public void ClearBureaucraticError(EntityUid passportUid, PassportComponent? comp = null)
    {
        if (!Resolve(passportUid, ref comp)) return;

        comp.HasBureaucraticError = false;
        comp.ErrorField           = string.Empty;
        comp.ErrorValue           = string.Empty;
        comp.Checked              = false;
        comp.CheckedBy            = string.Empty;
        comp.Stamp                = PassportStampState.None;

        Dirty(passportUid, comp);
        UpdateUiState(passportUid, comp);
    }

    private void TryApplyBureaucraticError(PassportComponent comp)
    {
        if (_random.Prob(BureaucraticErrorChance))
            ApplyBureaucraticError(comp);
    }

    private void ApplyBureaucraticError(PassportComponent comp)
    {
        var fields = new[] { "city", "passportnumber", "surname", "name", "issueddate" };
        var field  = _random.Pick(fields);

        comp.HasBureaucraticError = true;
        comp.Checked              = false;
        comp.CheckedBy            = string.Empty;
        comp.ErrorField           = field;
        comp.ErrorValue           = GenerateErrorValue(field, comp);
    }

    private string GenerateErrorValue(string field, PassportComponent comp)
    {
        switch (field)
        {
            case "city":
                var errorKey = _random.Pick(CityErrorKeys);
                return _loc.GetString(errorKey);

            case "passportnumber":
                return CorruptString(comp.PassportNumber);

            case "surname":
                return CorruptString(comp.Surname);

            case "name":
                return CorruptString(comp.OwnerName);

            case "issueddate":
                var date = DateTime.UtcNow;
                return _random.Pick(new[]
                {
                    date.ToString("MM.dd.yyyy"),
                    date.AddYears(-_random.Next(1, 5)).ToString("dd.MM.yyyy"),
                    date.AddMonths(_random.Next(1, 12)).ToString("dd.MM.yyyy"),
                });

            default:
                return "???";
        }
    }

    private string CorruptString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 2)
            return input + input;

        var chars = input.ToCharArray();
        var idx   = _random.Next(0, chars.Length - 1);

        switch (_random.Next(0, 3))
        {
            case 0: return input[..idx] + chars[idx] + input[idx..];
            case 1: return input[..idx] + input[(idx + 1)..];
            case 2:
                (chars[idx], chars[idx + 1]) = (chars[idx + 1], chars[idx]);
                return new string(chars);
            default: return input;
        }
    }

    private static bool IsImmuneJobId(string jobId)
        => ImmuneJobIds.Contains(jobId);

    private static string BuildSignature(string surname, string name)
    {
        if (name.Length == 0) return string.Empty;
        if (surname.Length == 0) return name;
        return surname + ", " + name[0] + ".";
    }

    private static string BuildMrz1(string passportNumber)
    {
        var num = string.Empty;
        foreach (var c in passportNumber)
            if (c != '-') num += c;
        return "BMRF" + num + "<<<<<<<<<";
    }

    private static string BuildMrz2(string surname, string name)
        => MrzPad(surname.ToUpper(), 14) + "<<" + MrzPad(name.ToUpper(), 10) + "<<<BM3<<<";

    private static string MrzPad(string input, int length)
        => input.Length >= length ? input[..length] : input + new string('<', length - input.Length);

    private static string GenerateNumber()
    {
        var rng = new Random();
        return "BM-" + rng.Next(1000, 9999) + "-" + rng.Next(100000, 999999);
    }
}
