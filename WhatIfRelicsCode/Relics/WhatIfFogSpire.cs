using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using WhatIfRelics.WhatIfRelicsCode.Ui;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfFogSpire")]
public class WhatIfFogSpire : WhatIfRelicModel
{
    private static int _mapFogUiSuppressDepth;

    public WhatIfFogSpire() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ApplyFogToMapScreen(Owner?.RunState, NMapScreen.Instance);
    }

    public static bool HasFogSpire(IRunState? runState)
    {
        return runState?.Players.Any(player => player.Relics.Any(static relic => relic is WhatIfFogSpire)) == true;
    }

    public static bool ShouldHideEnemyIntents(IRunState? runState)
    {
        return HasFogSpire(runState) && runState?.CurrentRoom is CombatRoom;
    }

    public static void PushMapFogUiSuppression()
    {
        _mapFogUiSuppressDepth++;
        RefreshMapFogUiSuppression();
    }

    public static void PopMapFogUiSuppression()
    {
        _mapFogUiSuppressDepth = Math.Max(0, _mapFogUiSuppressDepth - 1);
        RefreshMapFogUiSuppression();
    }

    private static void RefreshMapFogUiSuppression()
    {
        ApplyFogToMapScreen(
            NMapScreen.Instance == null
                ? null
                : WhatIfReflectionHelper.GetPrivateField<RunState>(NMapScreen.Instance, "_runState"),
            NMapScreen.Instance);
    }

    private static bool IsMapFogUiSuppressedEffective()
    {
        return _mapFogUiSuppressDepth > 0 || NCapstoneContainer.Instance?.InUse == true;
    }

    public static void ApplyFogToMapScreen(IRunState? runState, NMapScreen? screen)
    {
        if (screen == null)
        {
            return;
        }

        ActMap? map = runState?.Map ?? WhatIfReflectionHelper.GetPrivateField<ActMap>(screen, "_map");
        Dictionary<MapCoord, NMapPoint>? mapPoints = WhatIfReflectionHelper
            .GetPrivateField<Dictionary<MapCoord, NMapPoint>>(screen, "_mapPointDictionary");
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>? paths = WhatIfReflectionHelper
            .GetPrivateField<Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>>(screen, "_paths");
        if (map == null || mapPoints == null || paths == null)
        {
            return;
        }

        bool hasFogSpire = HasFogSpire(runState);
        int visibleRow = runState?.MapLocation.coord?.row ?? 0;
        int nextVisibleRow = visibleRow + 1;
        int lastMapRow = map.GetRowCount() - 1;
        bool showStartingPoint = runState?.MapLocation.coord == null;
        bool showBossNodes = nextVisibleRow > lastMapRow;
        int hiddenIndex = 0;

        foreach ((MapCoord coord, NMapPoint node) in mapPoints)
        {
            bool isVisible = !hasFogSpire || IsMapPointVisible(
                coord,
                map,
                visibleRow,
                nextVisibleRow,
                showStartingPoint,
                showBossNodes);

            if (isVisible)
            {
                RestoreVisibleNode(node);
            }
            else
            {
                ApplyHiddenFogNode(node, hiddenIndex++, IsMapFogUiSuppressedEffective());
            }
        }

        foreach (KeyValuePair<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> entry in paths)
        {
            MapCoord from = entry.Key.Item1;
            MapCoord to = entry.Key.Item2;
            IReadOnlyList<TextureRect> segments = entry.Value;
            bool visible = !hasFogSpire || (
                IsMapPointVisible(from, map, visibleRow, nextVisibleRow, showStartingPoint, showBossNodes)
                && IsMapPointVisible(to, map, visibleRow, nextVisibleRow, showStartingPoint, showBossNodes));
            foreach (TextureRect segment in segments)
            {
                segment.Visible = visible;
            }
        }
    }

    private static bool IsMapPointVisible(
        MapCoord coord,
        ActMap map,
        int visibleRow,
        int nextVisibleRow,
        bool showStartingPoint,
        bool showBossNodes)
    {
        if (coord == map.StartingMapPoint.coord)
        {
            return showStartingPoint;
        }

        if (coord == map.BossMapPoint.coord || (map.SecondBossMapPoint != null && coord == map.SecondBossMapPoint.coord))
        {
            return showBossNodes;
        }

        return coord.row == visibleRow || coord.row == nextVisibleRow;
    }

    private static void RestoreVisibleNode(NMapPoint node)
    {
        node.Visible = true;
        node.MouseFilter = Control.MouseFilterEnum.Stop;
        SetNodeChildVisible(node, "IconContainer", true);
        SetNodeChildVisible(node, "Icon", true);
        SetNodeChildVisible(node, "SpriteContainer", true);
        SetNodeChildVisible(node, "SelectionReticle", true);
        SetNodeChildVisible(node, "MapPointVoteContainer", true);
        SetVisitedMarkerVisible(node, true);
        WhatIfFogSpireMapPointFog.Remove(node);
    }

    private static void ApplyHiddenFogNode(NMapPoint node, int hiddenIndex, bool fogSuppressed)
    {
        node.Visible = true;
        node.MouseFilter = Control.MouseFilterEnum.Ignore;
        SetNodeChildVisible(node, "IconContainer", false);
        SetNodeChildVisible(node, "Icon", false);
        SetNodeChildVisible(node, "SpriteContainer", false);
        SetNodeChildVisible(node, "SelectionReticle", false);
        SetNodeChildVisible(node, "MapPointVoteContainer", false);
        SetVisitedMarkerVisible(node, false);

        (Vector2 size, Vector2 offset) = GetFogPlaceholderLayout(node);
        WhatIfFogSpireMapPointFog fog = WhatIfFogSpireMapPointFog.GetOrCreate(node, size, offset, hiddenIndex * 0.73f);
        fog.Visible = !fogSuppressed;
    }

    private static (Vector2 size, Vector2 offset) GetFogPlaceholderLayout(NMapPoint node)
    {
        return node switch
        {
            NBossMapPoint => (new Vector2(252f, 236f), new Vector2(61f, 35f)),
            NAncientMapPoint => (new Vector2(172f, 172f), new Vector2(18f, 18f)),
            _ => (new Vector2(104f, 104f), new Vector2(-24f, -24f))
        };
    }

    private static void SetNodeChildVisible(Node parent, string childName, bool visible)
    {
        NodePath[] candidates =
        [
            new NodePath($"%{childName}"),
            new NodePath(childName)
        ];

        foreach (NodePath candidate in candidates)
        {
            if (parent.GetNodeOrNull<CanvasItem>(candidate) is { } canvasItem)
            {
                canvasItem.Visible = visible;
                return;
            }
        }
    }

    private static void SetVisitedMarkerVisible(NMapPoint node, bool visible)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is NMapCircleVfx circleVfx)
            {
                circleVfx.Visible = visible;
            }
        }
    }
}
