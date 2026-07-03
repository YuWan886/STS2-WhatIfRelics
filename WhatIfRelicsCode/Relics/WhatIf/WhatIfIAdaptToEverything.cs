using System.Text.Json;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfIAdaptToEverything")]
public class WhatIfIAdaptToEverything : WhatIfRelicModel
{
    private const int DebuffImmunityThreshold = 5;

    private Dictionary<string, int> _attackAdaptationCounts = [];
    private Dictionary<string, int> _debuffAdaptationCounts = [];

    [SavedProperty]
    public string AttackAdaptationState
    {
        get => SerializeProgress(_attackAdaptationCounts);
        set
        {
            AssertMutable();
            _attackAdaptationCounts = DeserializeProgress(value);
        }
    }

    [SavedProperty]
    public string DebuffAdaptationState
    {
        get => SerializeProgress(_debuffAdaptationCounts);
        set
        {
            AssertMutable();
            _debuffAdaptationCounts = DeserializeProgress(value);
        }
    }

    public WhatIfIAdaptToEverything() : base(true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay = null)
    {
        if (target != Owner?.Creature || dealer == null || !dealer.IsEnemy || !props.IsPoweredAttack())
        {
            return 1m;
        }

        int requiredHits = GetRequiredAttackCount();
        if (requiredHits <= 1)
        {
            return 0m;
        }

        int priorHits = GetAttackAdaptationCount(dealer);
        decimal immunityRatio = Math.Clamp((decimal)priorHits / (requiredHits - 1), 0m, 1m);
        return 1m - immunityRatio;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        await base.AfterAttack(choiceContext, command);

        Creature? attacker = command.Attacker;
        Creature? ownerCreature = Owner?.Creature;
        if (attacker == null || ownerCreature == null || !attacker.IsEnemy || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }

        bool attackedOwner = command.Results
            .SelectMany(static hitResults => hitResults)
            .Any(result => result.Receiver == ownerCreature);
        if (!attackedOwner)
        {
            return;
        }

        IncrementAttackAdaptation(attacker);
        Flash();
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner?.Creature)
        {
            return false;
        }

        string? debuffKey = GetDebuffKey(canonicalPower, amount);
        if (debuffKey == null)
        {
            return false;
        }

        if (GetDebuffAdaptationCount(debuffKey) < DebuffImmunityThreshold)
        {
            return false;
        }

        modifiedAmount = 0m;
        return true;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner?.Creature)
        {
            return Task.CompletedTask;
        }

        string? debuffKey = GetDebuffKey(power, amount);
        if (debuffKey == null)
        {
            return Task.CompletedTask;
        }

        IncrementDebuffAdaptation(debuffKey);
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _attackAdaptationCounts.Clear();
        return Task.CompletedTask;
    }

    private int GetRequiredAttackCount()
    {
        return Owner?.RunState.CurrentRoom?.RoomType switch
        {
            RoomType.Boss => 12,
            RoomType.Elite => 7,
            _ => 4
        };
    }

    private int GetAttackAdaptationCount(Creature attacker)
    {
        string key = BuildCreatureKey(attacker);
        return _attackAdaptationCounts.GetValueOrDefault(key, 0);
    }

    private void IncrementAttackAdaptation(Creature attacker)
    {
        string key = BuildCreatureKey(attacker);
        int maxStoredCount = Math.Max(0, GetRequiredAttackCount() - 1);
        int current = _attackAdaptationCounts.GetValueOrDefault(key, 0);
        _attackAdaptationCounts[key] = Math.Min(maxStoredCount, current + 1);
    }

    private int GetDebuffAdaptationCount(string debuffKey)
    {
        return _debuffAdaptationCounts.GetValueOrDefault(debuffKey, 0);
    }

    private void IncrementDebuffAdaptation(string debuffKey)
    {
        int current = _debuffAdaptationCounts.GetValueOrDefault(debuffKey, 0);
        _debuffAdaptationCounts[debuffKey] = Math.Min(DebuffImmunityThreshold, current + 1);
    }

    private static string BuildCreatureKey(Creature creature)
    {
        string modelEntry = creature.Monster?.Id.Entry ?? creature.ModelId.Entry;
        string slotName = creature.SlotName ?? creature.CombatId?.ToString() ?? "unknown";
        return $"{modelEntry}|{slotName}";
    }

    private static string? GetDebuffKey(PowerModel power, decimal amount)
    {
        return power.GetTypeForAmount(amount) == PowerType.Debuff
            ? power.Id.Entry
            : null;
    }

    private static string SerializeProgress(Dictionary<string, int> progress)
    {
        if (progress.Count == 0)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(progress);
    }

    private static Dictionary<string, int> DeserializeProgress(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
