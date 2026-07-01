using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

// Shared holding pool for helper relics that must stay addressable via ModelDb
// without leaking into the normal shared relic/shop generation path.
internal sealed class WhatIfAuxRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "Purple";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return [];
    }
}
