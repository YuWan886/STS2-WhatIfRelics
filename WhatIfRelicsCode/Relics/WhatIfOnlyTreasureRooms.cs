using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyTreasureRooms")]
public class WhatIfOnlyTreasureRooms : WhatIfRelicModel
{
    public WhatIfOnlyTreasureRooms() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        ActMap? map = Owner?.RunState?.Map;
        if (map == null)
        {
            return;
        }

        ConvertRoomsToTreasure(map);
        RefreshNMapPoints(map);
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (runState.Players.Any(player => player.Relics.Any(relic => relic is WhatIfOnlyTreasureRooms)))
        {
            ConvertRoomsToTreasure(map);
        }

        return map;
    }

    private static void ConvertRoomsToTreasure(ActMap map)
    {
        int changed = 0;
        foreach (MapPoint point in map.GetAllMapPoints())
        {
            if (point.PointType is MapPointType.Ancient or MapPointType.Boss)
            {
                continue;
            }

            if (point.PointType == MapPointType.Treasure)
            {
                continue;
            }

            point.PointType = MapPointType.Treasure;
            changed++;
        }

        Entry.Logger.Info($"[WhatIfOnlyTreasureRooms] ConvertRoomsToTreasure: {changed} points changed to Treasure");
    }

    private static void RefreshNMapPoints(ActMap map)
    {
        NMapScreen? screen = NMapScreen.Instance;
        if (screen == null)
        {
            return;
        }

        IDictionary<MapCoord, NMapPoint>? dict = WhatIfReflectionHelper
            .GetPrivateField<IDictionary<MapCoord, NMapPoint>>(screen, "_mapPointDictionary");
        if (dict == null)
        {
            return;
        }

        foreach (MapPoint point in map.GetAllMapPoints())
        {
            if (dict.TryGetValue(point.coord, out NMapPoint? nPoint))
            {
                nPoint.RefreshVisualsInstantly();
            }
        }
    }
}
