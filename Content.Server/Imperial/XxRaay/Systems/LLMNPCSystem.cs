using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Localization;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class LLMNPCSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("llmnpc");
        SubscribeLocalEvent<LLMNPCComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<LLMNPCComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, LLMNPCComponent component, ComponentInit args)
    {
        component.IsGenerating = false;

        var listener = EnsureComp<ActiveListenerComponent>(uid);
        listener.Range = component.MaxDistanceTiles;
        
        if (string.IsNullOrWhiteSpace(component.NPCName))
        {
            component.NPCName = MetaData(uid).EntityName;
        }
    }

    private string GetDefaultSystemPrompt(LLMNPCComponent component)
    {
        var name = string.IsNullOrWhiteSpace(component.NPCName) ? "NPC" : component.NPCName;
        return Loc.GetString("llmnpc-default-system-prompt", ("name", name));
    }

    private void OnListen(Entity<LLMNPCComponent> ent, ref ListenEvent args)
    {
        var npcUid = ent.Owner;
        var npcComp = ent.Comp;

        if (npcUid == args.Source)
            return;

        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (npcComp.IsGenerating)
            return;

        if (string.IsNullOrEmpty(npcComp.ApiKey))
            return;

        var userMessage = args.Message;
        if (npcComp.IncludeContextInfo)
        {
            var contextInfo = GetContextInfo(args.Source, npcUid, npcComp);
            if (!string.IsNullOrWhiteSpace(contextInfo))
            {
                userMessage = Loc.GetString("llmnpc-context-message",
                    ("context", contextInfo),
                    ("message", args.Message));
            }
        }

        npcComp.MessageHistory.Add(new LLMMessageHistoryItem { Role = "user", Content = userMessage });
        
        while (npcComp.MessageHistory.Count > npcComp.MaxHistoryMessages)
        {
            npcComp.MessageHistory.RemoveAt(0);
        }

        _ = GenerateResponseAsync(npcUid, npcComp, userMessage);
    }

    private string GetContextInfo(EntityUid entity, EntityUid npcUid, LLMNPCComponent npcComp)
    {
        var infoParts = new List<string>();

        infoParts.Add(Loc.GetString("llmnpc-context-header-speaker"));
        
        var name = Identity.Name(entity, EntityManager);
        infoParts.Add(Loc.GetString("llmnpc-context-speaker-name", ("name", name)));

        if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid))
        {
            if (_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var species))
            {
                infoParts.Add(Loc.GetString("llmnpc-context-speaker-species", ("species", species.Name)));
            }
        }

        if (TryComp<DamageableComponent>(entity, out var damageable))
        {
            var currentHealth = GetCurrentHealth(entity, damageable);
            var maxHealth = GetMaxHealth(entity, damageable);
            infoParts.Add(Loc.GetString("llmnpc-context-speaker-health",
                ("current", currentHealth.ToString("F1")),
                ("max", maxHealth.ToString("F1"))));
        }

        if (_minds.TryGetMind(entity, out var mindId, out _))
        {
            if (_jobs.MindTryGetJobName(mindId, out var jobName))
            {
                infoParts.Add(Loc.GetString("llmnpc-context-speaker-job", ("job", jobName)));
            }
        }

        var equipment = new List<string>();
        if (TryComp<InventoryComponent>(entity, out var inventory))
        {
            var enumerator = _inventory.GetSlotEnumerator((entity, inventory));
            while (enumerator.NextItem(out var item, out var slot))
            {
                var itemName = MetaData(item).EntityName;
                equipment.Add($"{slot.Name}: {itemName}");
            }
        }

        if (equipment.Count > 0)
        {
            infoParts.Add(Loc.GetString("llmnpc-context-speaker-equipment",
                ("equipment", string.Join(", ", equipment))));
        }

        var handsItems = new List<string>();
        if (TryComp<HandsComponent>(entity, out var hands))
        {
            var handsEntity = (entity, hands);
            foreach (var handItem in _hands.EnumerateHeld(handsEntity))
            {
                var itemName = MetaData(handItem).EntityName;
                handsItems.Add(itemName);
            }
        }

        if (handsItems.Count > 0)
        {
            infoParts.Add(Loc.GetString("llmnpc-context-speaker-hands",
                ("items", string.Join(", ", handsItems))));
        }

        infoParts.Add("\n" + Loc.GetString("llmnpc-context-header-npc"));
        
        if (!string.IsNullOrWhiteSpace(npcComp.NPCName))
        {
            infoParts.Add(Loc.GetString("llmnpc-context-npc-name", ("name", npcComp.NPCName)));
        }

        if (TryComp<DamageableComponent>(npcUid, out var npcDamageable))
        {
            var npcCurrentHealth = GetCurrentHealth(npcUid, npcDamageable);
            var npcMaxHealth = GetMaxHealth(npcUid, npcDamageable);
            infoParts.Add(Loc.GetString("llmnpc-context-npc-health",
                ("current", npcCurrentHealth.ToString("F1")),
                ("max", npcMaxHealth.ToString("F1"))));
        }

        return string.Join("\n", infoParts);
    }

    private float GetCurrentHealth(EntityUid entity, DamageableComponent damageable)
    {
        var maxHealth = GetMaxHealth(entity, damageable);
        var currentHealth = maxHealth - (float)damageable.TotalDamage;
        return Math.Max(0f, currentHealth);
    }

    private float GetMaxHealth(EntityUid entity, DamageableComponent damageable)
    {
        if (TryComp<MobThresholdsComponent>(entity, out var thresholds))
        {
            if (thresholds.Thresholds.Count > 0)
            {
                return (float)thresholds.Thresholds.Keys.Max();
            }
        }

        if (damageable.HealthBarThreshold != null)
        {
            return (float)damageable.HealthBarThreshold.Value;
        }

        return 100f;
    }

    private async Task GenerateResponseAsync(EntityUid npcUid, LLMNPCComponent component, string message)
    {
        component.IsGenerating = true;

        try
        {
            var messages = new List<object>();
            
            var systemPrompt = component.SystemPrompt;
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = GetDefaultSystemPrompt(component);
            }
            
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            
            foreach (var historyMessage in component.MessageHistory)
            {
                messages.Add(new { role = historyMessage.Role, content = historyMessage.Content });
            }

            var requestBody = new
            {
                model = component.Model,
                messages = messages.ToArray()
            };

            var request = new HttpRequestMessage(HttpMethod.Post, component.ApiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Add("Authorization", $"Bearer {component.ApiKey}");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _sawmill.Error($"LLMNPC {npcUid}: API error [{response.StatusCode}]: {errorContent}");
                return;
            }

            var responseContent = await response.Content.ReadFromJsonAsync<GroqApiResponse>();

            if (responseContent?.Choices == null || responseContent.Choices.Length == 0)
            {
                _sawmill.Warning($"LLMNPC {npcUid}: Empty response from API");
                return;
            }

            var generatedMessage = responseContent.Choices[0].Message?.Content;
            if (string.IsNullOrWhiteSpace(generatedMessage))
            {
                _sawmill.Warning($"LLMNPC {npcUid}: Empty message content in response");
                return;
            }

            component.MessageHistory.Add(new LLMMessageHistoryItem { Role = "assistant", Content = generatedMessage });
            
            while (component.MessageHistory.Count > component.MaxHistoryMessages)
            {
                component.MessageHistory.RemoveAt(0);
            }

            _chatSystem.TrySendInGameICMessage(npcUid, generatedMessage, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }
        catch (HttpRequestException ex)
        {
            _sawmill.Error($"LLMNPC {npcUid}: HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            _sawmill.Warning($"LLMNPC {npcUid}: Request timeout");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"LLMNPC {npcUid}: Error generating response: {ex}");
        }
        finally
        {
            component.IsGenerating = false;
        }
    }
}

