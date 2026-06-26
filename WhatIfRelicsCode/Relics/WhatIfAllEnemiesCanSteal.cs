using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfAllEnemiesCanSteal")]
public class WhatIfAllEnemiesCanSteal : WhatIfRelicModel
{
    private static readonly Func<CardModel, bool>[] StealPriorities =
    [
        static c => c.Enchantment is not Imbued && c.Rarity == CardRarity.Uncommon,
        static c => c.Enchantment is not Imbued && c.Rarity is CardRarity.Common or CardRarity.Rare or CardRarity.Event,
        static c => c.Enchantment is not Imbued && c.Rarity is CardRarity.Basic or CardRarity.Quest,
        static c => c.Rarity == CardRarity.Ancient || c.Enchantment is Imbued
    ];

    public WhatIfAllEnemiesCanSteal() : base(true)
    {
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        await base.AfterAttack(choiceContext, command);

        Creature? attacker = command.Attacker;
        if (attacker == null || !attacker.IsEnemy || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }
        Creature enemy = attacker;

        Creature? ownerCreature = Owner?.Creature;
        if (ownerCreature == null || ownerCreature.IsDead)
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

        CardModel? stolenCard = ChooseCardToSteal();
        if (stolenCard?.DeckVersion == null)
        {
            return;
        }

        await CardPileCmd.RemoveFromCombat(stolenCard);

        SwipePower swipe = (SwipePower)ModelDb.Power<SwipePower>().ToMutable();
        await swipe.Steal(stolenCard);
        await PowerCmd.Apply(choiceContext, swipe, enemy, 1m, enemy, null);
        Flash();
    }

    private CardModel? ChooseCardToSteal()
    {
        if (Owner == null)
        {
            return null;
        }

        List<CardModel> cards = CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)
            .Where(static card => card.DeckVersion != null)
            .ToList();
        if (cards.Count == 0)
        {
            return null;
        }

        IEnumerable<CardModel> candidatePool = cards;
        foreach (Func<CardModel, bool> predicate in StealPriorities)
        {
            List<CardModel> prioritized = cards.Where(predicate).ToList();
            if (prioritized.Count == 0)
            {
                continue;
            }

            candidatePool = prioritized;
            break;
        }

        return Owner.RunState.Rng.CombatCardGeneration.NextItem(candidatePool);
    }
}
