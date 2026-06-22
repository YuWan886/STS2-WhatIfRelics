using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Localization;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.HoverTip), MethodType.Getter)]
internal static class WhatIfRelicHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(RelicModel __instance, ref HoverTip __result)
    {
        if (__instance is not WhatIfRelicModel whatIfRelic)
        {
            return;
        }

        string description = WhatIfRelicDescriptionBuilder.Build(whatIfRelic);
        __result = new HoverTip(whatIfRelic.Title, description, whatIfRelic.Icon);
        __result.SetCanonicalModel(whatIfRelic.CanonicalInstance);
    }
}
