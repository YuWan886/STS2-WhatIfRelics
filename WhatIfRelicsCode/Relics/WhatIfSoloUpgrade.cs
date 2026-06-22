using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSoloUpgrade")]
public class WhatIfSoloUpgrade : WhatIfRelicModel
{
    public WhatIfSoloUpgrade() : base(true)
    {
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Deck == null || Owner.Creature.IsDead)
        {
            return Task.CompletedTask;
        }

        var upgradableCards = Owner.Deck.Cards
            .Where(card => card.IsUpgradable)
            .ToList();

        if (upgradableCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        CardModel? upgraded = Owner.PlayerRng.Rewards.NextItem(upgradableCards);
        if (upgraded == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        CardCmd.Upgrade(upgraded, CardPreviewStyle.MessyLayout);
        return Task.CompletedTask;
    }
}
