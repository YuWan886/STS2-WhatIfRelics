using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfDeckTransforms")]
public sealed class WhatIfDeckTransforms : WhatIfRelicModel
{
    private int _lastTransformedRoomCount = -1;

    [SavedProperty]
    public int LastTransformedRoomCount
    {
        get => _lastTransformedRoomCount;
        private set
        {
            AssertMutable();
            _lastTransformedRoomCount = Math.Max(-1, value);
        }
    }

    public WhatIfDeckTransforms() : base(true)
    {
    }

    public override async Task BeforeRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return;
        }

        if (Owner?.Deck == null)
        {
            return;
        }

        int currentRoomCount = Owner.RunState.CurrentRoomCount;
        if (currentRoomCount == LastTransformedRoomCount)
        {
            return;
        }

        List<CardModel> originalCards = Owner.Deck.Cards
            .Where(static card => card.IsTransformable)
            .ToList();
        if (originalCards.Count == 0)
        {
            LastTransformedRoomCount = currentRoomCount;
            return;
        }

        List<CardModel> allPoolCandidates = GetGlobalTransformationCandidates(Owner);
        if (allPoolCandidates.Count == 0)
        {
            LastTransformedRoomCount = currentRoomCount;
            return;
        }

        IEnumerable<CardTransformation> transformations = originalCards
            .Select(card => new CardTransformation(card, allPoolCandidates));

        Flash();
        await CardCmd.Transform(transformations, Owner.RunState.Rng.Niche, CardPreviewStyle.None);
        LastTransformedRoomCount = currentRoomCount;
    }

    private static List<CardModel> GetGlobalTransformationCandidates(Player player)
    {
        return ModelDb.AllCardPools
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .DistinctBy(static card => card.Id.Entry)
            .ToList();
    }
}
