using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(RunManager), "CreateRoom", typeof(RoomType), typeof(MapPointType), typeof(AbstractModel))]
internal static class WhatIfOnlyFakeMerchantShopsPatch
{
    [HarmonyPostfix]
    private static void Postfix(RoomType roomType, ref AbstractRoom __result)
    {
        if (roomType != RoomType.Shop)
        {
            return;
        }

        if (!WhatIfOnlyFakeMerchantShops.HasFakeMerchantShops(RunManager.Instance?.State))
        {
            return;
        }

        __result = new EventRoom(ModelDb.Event<FakeMerchant>());
    }
}
