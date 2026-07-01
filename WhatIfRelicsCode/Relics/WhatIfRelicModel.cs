using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Assets;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public abstract class WhatIfRelicModel : RelicModel
{
    private const string RelicImageRoot = "res://WhatIfRelics/images/relics";

    public sealed override RelicRarity Rarity => RelicRarity.Event;

    public override int MerchantCost => 999999999;

    public override bool IsAllowedInShops => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (this is not IWhatIfUniformRelicSource uniformRelicSource)
            {
                return [];
            }

            RelicModel? previewRelic = uniformRelicSource.GetUniformRelicForHoverTips();
            if (previewRelic == null && IsMutable && Owner?.RunState is { } runState)
            {
                previewRelic = uniformRelicSource.GetUniformRelic(runState);
            }

            if (previewRelic == null || ReferenceEquals(previewRelic, this))
            {
                return [];
            }

            return previewRelic.HoverTips;
        }
    }

    protected override string BigIconPath => ResolveAutoIconPath();

    public override string PackedIconPath => ResolveAutoIconPath();

    protected override string PackedIconOutlinePath => ResolveAutoIconPath();

    protected WhatIfRelicModel()
    {
    }

    protected WhatIfRelicModel(bool autoAdd) : this()
    {
    }

    private string ResolveAutoIconPath()
    {
        return WhatIfAssetPathHelper.ResolveExistingPath(
            WhatIfAssetPathHelper.BuildAutoImagePath(GetType(), RelicImageRoot),
            WhatIfAssetPathHelper.PlaceholderRelicIconPath);
    }

    public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
    {
        if (player != Owner || cards.Count == 0 || !WhatIfReplacementContext.ShouldReplaceCardRewards(CardCreationSource.Shop))
        {
            return;
        }

        bool alreadyModifiedByThisRelic = cards.All(card =>
            card.ModifyingRelics.Any(modifyingRelic => ReferenceEquals(modifyingRelic, this)));
        if (alreadyModifiedByThisRelic)
        {
            return;
        }

        var originals = cards.ToArray();
        CardCreationOptions options = CardCreationOptions.ForRoom(player, RoomType.Shop);
        if (!TryModifyCardRewardOptions(player, cards, options))
        {
            return;
        }

        for (int i = 0; i < cards.Count && i < originals.Length; i++)
        {
            if (!ReferenceEquals(cards[i], originals[i]))
            {
                originals[i].ModifyCard(cards[i].Card, this);
                cards[i] = originals[i];
            }
        }
    }
}




