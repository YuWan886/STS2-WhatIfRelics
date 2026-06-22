using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class WhatIfQuestionMarkMapPatch
{
    [HarmonyPrefix]
    public static void Prefix(ActMap map)
    {
        var state = RunManager.Instance?.State;
        if (state == null || map == null)
        {
            return;
        }

        var hasRelic = state.Players.Any(player => player.Relics.Any(static relic => relic is WhatIfQuestionMark));
        if (hasRelic)
        {
            WhatIfQuestionMark.ForceMapToUnknown(map);
        }
    }
}
