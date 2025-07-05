using Content.Server.Imperial.Revolutionary.Components;
using Content.Server.Imperial.Revolutionary.UI;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Shared.Imperial.Revolutionary.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Content.Shared.Zombies;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Revolutionary
{
    /// <summary>
    /// Система, управляющая обращением в революционеры с согласием.
    /// </summary>
    public sealed class ConsentRevolutionarySystem : EntitySystem
    {
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly RevolutionaryRuleSystem _revolutionaryRuleSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

        private float _timeAccumulator = 0f;
        private const float UpdateInterval = 1f;

        public const string ConversionDeniedStatusEffect = "RevConversionDenied";
        public const string ConversionCooldownStatusEffect = "RevConversionCooldown";

        public override void Initialize()
        {
            base.Initialize();

            // Подписка на события, связанные с обращением
            SubscribeLocalEvent<ConsentRevolutionaryComponent, MobStateChangedEvent>(HandleMobStateChanged);
            SubscribeLocalEvent<ConsentRevolutionaryComponent, MindRemovedMessage>(HandleMindRemoved);
            SubscribeLocalEvent<ConsentRevolutionaryComponent, RoleAddedEvent>(HandleRoleAdded);
            SubscribeLocalEvent<ConsentRevolutionaryComponent, RemoveConversionDeniedAlertEvent>(HandleRemoveConversionDeniedAlert);
            SubscribeLocalEvent<HeadRevolutionaryComponent, GetVerbsEvent<InnateVerb>>(HandleInnateVerb);
        }

        /// <summary>
        /// Обработка события удаления алерта отказа обращения
        /// </summary>
        private void HandleRemoveConversionDeniedAlert(Entity<ConsentRevolutionaryComponent> entity, ref RemoveConversionDeniedAlertEvent args)
        {
            _statusEffectsSystem.TryRemoveStatusEffect(entity.Owner, ConversionDeniedStatusEffect);
        }

        /// <summary>
        /// Обработка добавления глагола обращения
        /// </summary>
        private void HandleInnateVerb(EntityUid userUid, HeadRevolutionaryComponent headComp, GetVerbsEvent<InnateVerb> verbArgs)
        {
            // Проверяем возможность обращения цели
            if (!headComp.OnlyConsentConvert
                || !headComp.ConvertAbilityEnabled
                || !verbArgs.CanAccess
                || !verbArgs.CanInteract
                || HasComp<RevolutionaryComponent>(verbArgs.Target)
                || !_mobStateSystem.IsAlive(verbArgs.Target)
                || HasComp<ZombieComponent>(verbArgs.Target)
                || HasComp<ZombieComponent>(userUid)) // Добавлена проверка, что сам глава революции не зомби
            {
                return;
            }

            if (IsEntityInConversionProcess(verbArgs.Target) || IsEntityInConversionProcess(verbArgs.User))
                return;

            var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(verbArgs.Target);

            if ((!HasComp<HumanoidAppearanceComponent>(verbArgs.Target) ||
                 !_mindSystem.TryGetMind(verbArgs.Target, out var mindId, out var mind))
                && !alwaysConvertible)
            {
                return;
            }

            InnateVerb verb;

            if (HasComp<ConsentRevolutionaryCooldownComponent>(verbArgs.User))
            {
                // Если у конвертера есть кулдаун, показываем неактивный глагол
                verb = new InnateVerb
                {
                    Disabled = true,
                    Text = Loc.GetString("rev-verb-consent-convert-text"),
                    Message = Loc.GetString("rev-verb-consent-convert-message-cooldown"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
                };
            }
            else
            {
                // Активный глагол для обращения
                verb = new InnateVerb
                {
                    Act = () =>
                    {
                        if (TryComp<ConsentRevolutionaryDenyComponent>(verbArgs.Target, out var denyComp))
                        {
                            _popupSystem.PopupEntity(
                                Loc.GetString(denyComp.OnConversionAttemptText, ("target", Identity.Entity(verbArgs.Target, EntityManager))),
                                verbArgs.Target,
                                verbArgs.User);
                            return;
                        }

                        if (HasComp<MindShieldComponent>(verbArgs.Target) ||
                            HasComp<ZombieComponent>(verbArgs.Target))
                        {
                            _popupSystem.PopupEntity(
                                Loc.GetString("rev-consent-convert-attempted-to-be-converted", ("user", Identity.Entity(verbArgs.User, EntityManager))),
                                verbArgs.User,
                                verbArgs.Target,
                                PopupType.MediumCaution);
                            _popupSystem.PopupEntity(
                                Loc.GetString("rev-consent-convert-failed", ("target", Identity.Entity(verbArgs.Target, EntityManager))),
                                verbArgs.Target,
                                verbArgs.User,
                                PopupType.MediumCaution);
                            return;
                        }

                        if (!_revolutionaryRuleSystem.IsConvertable(verbArgs.Target))
                            return;

                        RequestConsentConversion(verbArgs.Target, verbArgs.User);
                    },
                    Text = Loc.GetString("rev-verb-consent-convert-text"),
                    Message = Loc.GetString("rev-verb-consent-convert-message"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
                };
            }

            verbArgs.Verbs.Add(verb);
        }

        public override void Update(float frameTime)
        {
            _timeAccumulator += frameTime;

            if (_timeAccumulator < UpdateInterval)
                return;

            _timeAccumulator -= UpdateInterval;

            var query = EntityQueryEnumerator<ConsentRevolutionaryComponent>();
            while (query.MoveNext(out var uid, out var consentComp))
            {
                if (consentComp.IsConverter || consentComp.OtherMember == null)
                    continue;

                if (!TryComp<ConsentRevolutionaryComponent>(consentComp.OtherMember, out var otherConsentComp))
                {
                    consentComp.OtherMember = null;
                    continue;
                }

                if (consentComp.RequestStartTime != null &&
                    _gameTiming.CurTime - consentComp.RequestStartTime > consentComp.ResponseTime)
                {
                    CancelConsentRequest(uid,
                        consentComp.OtherMember.Value,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-timeout"));

                    continue;
                }

                if (!_transformSystem.InRange(Transform(uid).Coordinates,
                        Transform(consentComp.OtherMember.Value).Coordinates,
                        consentComp.MaxDistance))
                {
                    CancelConsentRequest(uid,
                        consentComp.OtherMember.Value,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-out-of-range"));

                    continue;
                }
            }
        }

        /// <summary>
        /// Обработка события изменения состояния
        /// </summary>
        private void HandleMobStateChanged(EntityUid uid, ConsentRevolutionaryComponent consentComp, MobStateChangedEvent args)
        {
            if (consentComp.OtherMember == null || !TryComp<ConsentRevolutionaryComponent>(consentComp.OtherMember, out var otherConsentComp))
                return;

            if (args.NewMobState == MobState.Alive)
                return;

                if (consentComp.IsConverter)
                {
                    CancelConsentRequest(consentComp.OtherMember.Value,
                        uid,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
                }
                else
                {
                    CancelConsentRequest(uid,
                        consentComp.OtherMember.Value,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
                }
        }

        /// <summary>
        /// Обработка события МШ
        /// </summary>
        private void HandleMindRemoved(Entity<ConsentRevolutionaryComponent> entity, ref MindRemovedMessage args)
        {
            if (entity.Comp.OtherMember == null || !TryComp<ConsentRevolutionaryComponent>(entity.Comp.OtherMember, out var otherConsentComp))
                return;

                if (entity.Comp.IsConverter)
                {
                    CancelConsentRequest(entity.Comp.OtherMember.Value,
                        entity.Owner,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
                }
                else
                {
                    CancelConsentRequest(entity.Owner,
                        entity.Comp.OtherMember.Value,
                        Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));

                }
        }

        /// <summary>
        /// Обработка события добавления роли
        /// </summary>
        private void HandleRoleAdded(Entity<ConsentRevolutionaryComponent> entity, ref RoleAddedEvent args)
        {
            if (entity.Comp.OtherMember == null ||
                entity.Comp.IsConverter ||
                !TryComp<ConsentRevolutionaryComponent>(entity.Comp.OtherMember, out var otherConsentComp) ||
                !HasComp<RevolutionaryComponent>(entity))
                return;

            CancelConsentRequest(entity.Comp.OtherMember.Value, entity.Owner);

        }

        /// <summary>
        /// Запрос на обращение сущности в революционера с согласием
        /// </summary>
        /// <param name="target">Цель обращения</param>
        /// <param name="converter">Инициатор обращения</param>
        public void RequestConsentConversion(EntityUid target, EntityUid converter)
        {
            if (TryComp<ActorComponent>(target, out var actorComponent))
            {
                var session = actorComponent.PlayerSession;

                _popupSystem.PopupEntity(
                    Loc.GetString("rev-consent-convert-requested", ("target", Identity.Entity(target, EntityManager))),
                    converter,
                    converter);

                var consentWindow = new ConsentRequestedEui(target, converter, _revolutionaryRuleSystem, this, _popupSystem, EntityManager);

                var targetComp = EnsureComp<ConsentRevolutionaryComponent>(target);
                targetComp.OtherMember = converter;
                targetComp.Window = consentWindow;
                targetComp.RequestStartTime = _gameTiming.CurTime;
                targetComp.IsConverter = false;

                var converterComp = EnsureComp<ConsentRevolutionaryComponent>(converter);
                converterComp.OtherMember = target;
                converterComp.IsConverter = true;

                _euiManager.OpenEui(consentWindow, session);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("rev-consent-convert-auto-accepted", ("target", Identity.Entity(target, EntityManager))),
                    converter,
                    converter);
                _revolutionaryRuleSystem.ConvertEntityToRevolution(target, converter);
            }
        }

        /// <summary>
        /// Проверяет, находится ли сущность в процессе обращения
        /// </summary>
        /// <param name="entity">Сущность для проверки</param>
        /// <returns>True, если в процессе обращения</returns>
        public bool IsEntityInConversionProcess(EntityUid entity)
        {
            return TryComp<ConsentRevolutionaryComponent>(entity, out var consentComp)
                   && consentComp.OtherMember != null;
        }

        /// <summary>
        /// Применяет кулдаун к конвертеру после обращения
        /// </summary>
        /// <param name="converterUid">Сущность конвертера</param>
        public void ApplyConversionCooldown(EntityUid converterUid)
        {
            if (!TryComp<ConsentRevolutionaryComponent>(converterUid, out var comp))
                return;

            _statusEffectsSystem.TryAddStatusEffect<ConsentRevolutionaryCooldownComponent>(converterUid,
                ConversionCooldownStatusEffect,
                comp.ConversionBlockTime,
                true);
        }

        /// <summary>
        /// Применяет блокировку обращения к цели после отказа
        /// </summary>
        /// <param name="targetEntity">Сущность цели</param>
        public void ApplyConversionDeny(Entity<ConsentRevolutionaryComponent> targetEntity)
        {
            _statusEffectsSystem.TryAddStatusEffect<ConsentRevolutionaryDenyComponent>(targetEntity,
                ConversionDeniedStatusEffect,
                targetEntity.Comp.RequestBlockTime,
                true);
        }

        /// <summary>
        /// Отменяет запрос на обращение
        /// </summary>
        /// <param name="targetEntity">Цель запроса</param>
        /// <param name="converterEntity">Инициатор запроса</param>
        /// <param name="reason">Причина отмены, показываемая в попапах</param>
        public void CancelConsentRequest(EntityUid targetUid, EntityUid converterUid, string? reason = null)
        {
            if (!TryComp<ConsentRevolutionaryComponent>(targetUid, out var targetComp) ||
                !TryComp<ConsentRevolutionaryComponent>(converterUid, out var converterComp))
                return;

            if (reason != null)
            {
                _popupSystem.PopupEntity(reason, targetUid, targetUid, PopupType.MediumCaution);
                _popupSystem.PopupEntity(reason, converterUid, converterUid, PopupType.MediumCaution);
            }

            targetComp.OtherMember = null;

            if (targetComp.Window != null)
            {
                targetComp.Window.Close();
                targetComp.Window = null;
            }

            targetComp.RequestStartTime = null;

            converterComp.OtherMember = null;
        }
    }
}
