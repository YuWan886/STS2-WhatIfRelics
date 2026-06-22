using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Localization;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Description), MethodType.Getter)]
internal static class WhatIfRelicDescriptionPatch
{
    [HarmonyPostfix]
    private static void Postfix(RelicModel __instance, ref LocString __result)
    {
        if (__instance is not WhatIfRelicModel whatIfRelic)
        {
            return;
        }

        __result = WhatIfRelicDescriptionBuilder.BuildLocString(whatIfRelic);
    }
}
