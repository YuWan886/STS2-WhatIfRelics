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
    private string _lastTransformedRoomKey = string.Empty;

    [SavedProperty]
    public string LastTransformedRoomKey
    {
        get => _lastTransformedRoomKey;
        private set
        {
            AssertMutable();
            _lastTransformedRoomKey = value ?? string.Empty;
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

        string roomKey = BuildRoomKey(Owner, room);
        if (roomKey == LastTransformedRoomKey)
        {
            return;
        }

        List<CardModel> originalCards = Owner.Deck.Cards
            .Where(static card => card.IsTransformable)
            .ToList();
        if (originalCards.Count == 0)
        {
            LastTransformedRoomKey = roomKey;
            return;
        }

        List<CardModel> allPoolCandidates = GetGlobalTransformationCandidates(Owner);
        if (allPoolCandidates.Count == 0)
        {
            LastTransformedRoomKey = roomKey;
            return;
        }

        List<CardTransformation> transformations = originalCards
            .Select(card => new CardTransformation(card, CreateReplacementCard(card, allPoolCandidates)))
            .ToList();

        Flash();
        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
        LastTransformedRoomKey = roomKey;
    }

    private static List<CardModel> GetGlobalTransformationCandidates(Player player)
    {
        return ModelDb.AllCardPools
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .DistinctBy(static card => card.Id.Entry)
            .ToList();
    }

    private CardModel CreateReplacementCard(CardModel original, List<CardModel> candidates)
    {
        CardModel picked = candidates[Owner!.RunState.Rng.Niche.NextInt(candidates.Count)];
        CardModel replacement = Owner.RunState.CreateCard(picked, Owner);

        int upgradesToApply = Math.Min(original.CurrentUpgradeLevel, replacement.MaxUpgradeLevel);
        for (int i = 0; i < upgradesToApply; i++)
        {
            replacement.UpgradeInternal();
            replacement.FinalizeUpgradeInternal();
        }

        return replacement;
    }

    private static string BuildRoomKey(Player player, AbstractRoom room)
    {
        var location = player.RunState.MapLocation;
        string roomEntry = room.ModelId?.Entry ?? room.RoomType.ToString();
        int runtimeRoomId = room.Id ?? -1;
        int roomDepth = player.RunState.CurrentRoomCount;
        return $"{location.actIndex}|{location.coord?.col ?? -1}|{location.coord?.row ?? -1}|{roomDepth}|{runtimeRoomId}|{roomEntry}";
    }
}
