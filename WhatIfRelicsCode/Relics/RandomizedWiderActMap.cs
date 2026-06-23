using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public sealed class RandomizedWiderActMap : ActMap
{
    private static readonly HashSet<MapPointType> LowerMapPointRestrictions =
    [
        MapPointType.RestSite,
        MapPointType.Elite
    ];

    private static readonly HashSet<MapPointType> UpperMapPointRestrictions =
    [
        MapPointType.RestSite
    ];

    private static readonly HashSet<MapPointType> ParentMapPointRestrictions =
    [
        MapPointType.Elite,
        MapPointType.RestSite,
        MapPointType.Treasure,
        MapPointType.Shop
    ];

    private static readonly HashSet<MapPointType> ChildMapPointRestrictions =
    [
        MapPointType.Elite,
        MapPointType.RestSite,
        MapPointType.Treasure,
        MapPointType.Shop
    ];

    private static readonly HashSet<MapPointType> SiblingPointTypeRestrictions =
    [
        MapPointType.RestSite,
        MapPointType.Monster,
        MapPointType.Unknown,
        MapPointType.Elite,
        MapPointType.Shop
    ];

    private MapPoint?[,] _grid;
    private readonly MapPoint _bossPoint;
    private readonly MapPoint _startingPoint;
    private readonly MapPoint? _secondBossPoint;
    private readonly Rng _rng;
    private readonly int _mapWidth;
    private readonly int _mapLength;
    private readonly bool _shouldReplaceTreasureWithElites;
    private readonly int _targetExtraRests;
    private readonly int _targetShops;
    private readonly int _targetElites;
    private readonly int _targetUnknowns;

    public RandomizedWiderActMap(IRunState runState, ActMap original)
    {
        _mapLength = original.GetRowCount();
        _mapWidth = Math.Max(original.GetColumnCount() + 5, (int)Math.Ceiling(original.GetColumnCount() * 1.75));
        _rng = new Rng(runState.Rng.Seed, $"what_if_wider_random_map_act_{runState.CurrentActIndex + 1}_map");
        _grid = new MapPoint[_mapWidth, _mapLength];
        _bossPoint = new MapPoint(_mapWidth / 2, _mapLength)
        {
            PointType = MapPointType.Boss
        };
        _startingPoint = new MapPoint(_mapWidth / 2, 0)
        {
            PointType = original.StartingMapPoint.PointType,
            CanBeModified = original.StartingMapPoint.CanBeModified
        };

        if (original.SecondBossMapPoint != null)
        {
            _secondBossPoint = new MapPoint(_mapWidth / 2, _mapLength + 1)
            {
                PointType = MapPointType.Boss
            };
        }

        _shouldReplaceTreasureWithElites = ShouldReplaceTreasureWithElites(original);
        (_targetExtraRests, _targetShops, _targetElites, _targetUnknowns) = BuildScaledTargets(original);

        GenerateTopology();
        AssignPointTypes();

        _grid = MapPostProcessing.CenterGrid(_grid);
        _grid = MapPostProcessing.SpreadAdjacentMapPoints(_grid);
        _grid = MapPostProcessing.StraightenPaths(_grid);

        Entry.Logger.Info(
            $"[RandomizedWiderActMap] Randomized wider map generated: width={_mapWidth}, rows={_mapLength}, nodes={GetAllMapPoints().Count()}");
    }

    public override MapPoint BossMapPoint => _bossPoint;

    public override MapPoint StartingMapPoint => _startingPoint;

    public override MapPoint? SecondBossMapPoint => _secondBossPoint;

    protected override MapPoint?[,] Grid => _grid;

    private void GenerateTopology()
    {
        int iterations = _mapWidth + 4;
        for (int i = 0; i < iterations; i++)
        {
            MapPoint start = GetOrCreatePoint(GetRandomStartColumn(i), 1);
            startMapPoints.Add(start);
            PathGenerate(start);
        }

        ExpandBranchesRandomly();

        ForEachInRow(_mapLength - 1, static (point, self) => point.AddChildPoint(self._bossPoint), this);
        if (_secondBossPoint != null)
        {
            _bossPoint.AddChildPoint(_secondBossPoint);
        }

        ForEachInRow(1, static (point, self) => self._startingPoint.AddChildPoint(point), this);
    }

    private int GetRandomStartColumn(int iteration)
    {
        if (iteration < _mapWidth)
        {
            return iteration;
        }

        return _rng.NextInt(0, _mapWidth);
    }

    private void ExpandBranchesRandomly()
    {
        for (int row = 1; row < _mapLength - 1; row++)
        {
            List<MapPoint> rowPoints = GetPointsInRow(row).ToList();
            _rng.Shuffle(rowPoints);

            foreach (MapPoint point in rowPoints)
            {
                if (!ShouldAddBranch(point))
                {
                    continue;
                }

                if (TryAddExtraBranch(point, out MapPoint? createdBranch) && createdBranch != null && createdBranch.Children.Count == 0)
                {
                    PathGenerate(createdBranch);
                }
            }
        }
    }

    private bool ShouldAddBranch(MapPoint point)
    {
        if (point.coord.row >= _mapLength - 1)
        {
            return false;
        }

        if (point.Children.Count >= 3)
        {
            return false;
        }

        float chance = point.coord.row <= 4 ? 0.8f : 0.55f;
        return _rng.NextFloat() < chance;
    }

    private bool TryAddExtraBranch(MapPoint point, out MapPoint? createdBranch)
    {
        createdBranch = null;
        int nextRow = point.coord.row + 1;
        List<int> candidateColumns = Enumerable
            .Range(Math.Max(0, point.coord.col - 1), Math.Min(_mapWidth - 1, point.coord.col + 1) - Math.Max(0, point.coord.col - 1) + 1)
            .Where(col => col != point.coord.col || point.Children.Count == 0)
            .ToList();
        _rng.Shuffle(candidateColumns);

        candidateColumns = candidateColumns
            .OrderBy(col => GetPoint(col, nextRow)?.parents.Count ?? 0)
            .ThenBy(col => Math.Abs(col - point.coord.col))
            .ToList();

        foreach (int col in candidateColumns)
        {
            if (HasInvalidCrossover(point, col))
            {
                continue;
            }

            MapPoint child = GetOrCreatePoint(col, nextRow);
            if (point.Children.Contains(child))
            {
                continue;
            }

            point.AddChildPoint(child);
            createdBranch = child;
            return true;
        }

        return false;
    }

    private void PathGenerate(MapPoint startingPoint)
    {
        MapPoint current = startingPoint;
        while (current.coord.row < _mapLength - 1)
        {
            MapCoord nextCoord = GenerateNextCoord(current);
            MapPoint next = GetOrCreatePoint(nextCoord.col, nextCoord.row);
            current.AddChildPoint(next);
            current = next;
        }
    }

    private MapCoord GenerateNextCoord(MapPoint current)
    {
        int row = current.coord.row + 1;
        List<int> candidateColumns =
        [
            Math.Max(0, current.coord.col - 1),
            current.coord.col,
            Math.Min(_mapWidth - 1, current.coord.col + 1)
        ];

        candidateColumns = candidateColumns.Distinct().ToList();
        _rng.Shuffle(candidateColumns);
        candidateColumns = candidateColumns
            .OrderBy(col => GetPoint(col, row)?.parents.Count ?? 0)
            .ThenBy(col => Math.Abs(col - current.coord.col))
            .ToList();

        foreach (int col in candidateColumns)
        {
            if (!HasInvalidCrossover(current, col))
            {
                return new MapCoord
                {
                    col = col,
                    row = row
                };
            }
        }

        return new MapCoord
        {
            col = current.coord.col,
            row = row
        };
    }

    private bool HasInvalidCrossover(MapPoint current, int targetColumn)
    {
        int delta = targetColumn - current.coord.col;
        if (delta == 0)
        {
            return false;
        }

        MapPoint? neighbor = _grid[targetColumn, current.coord.row];
        if (neighbor == null)
        {
            return false;
        }

        foreach (MapPoint child in neighbor.Children)
        {
            if (child.coord.col - neighbor.coord.col == -delta)
            {
                return true;
            }
        }

        return false;
    }

    private void AssignPointTypes()
    {
        ForEachInRow(_mapLength - 1, static (point, _) =>
        {
            point.PointType = MapPointType.RestSite;
            point.CanBeModified = false;
        }, this);

        int treasureRow = _mapLength - 7;
        if (treasureRow > 1 && treasureRow < _mapLength)
        {
            MapPointType fixedType = _shouldReplaceTreasureWithElites ? MapPointType.Elite : MapPointType.Treasure;
            ForEachInRow(treasureRow, static (point, self) =>
            {
                point.PointType = self._shouldReplaceTreasureWithElites ? MapPointType.Elite : MapPointType.Treasure;
                point.CanBeModified = false;
            }, this);
        }

        ForEachInRow(1, static (point, _) =>
        {
            point.PointType = MapPointType.Monster;
            point.CanBeModified = false;
        }, this);

        Queue<MapPointType> pointTypesToAssign = BuildPointTypeQueue();
        AssignRemainingTypesToRandomPoints(pointTypesToAssign);

        foreach (MapPoint point in GetAllMapPoints().Where(static point => point.PointType == MapPointType.Unassigned))
        {
            point.PointType = MapPointType.Monster;
        }
    }

    private Queue<MapPointType> BuildPointTypeQueue()
    {
        List<MapPointType> types = [];

        for (int i = 0; i < _targetExtraRests; i++)
        {
            types.Add(MapPointType.RestSite);
        }

        for (int i = 0; i < _targetShops; i++)
        {
            types.Add(MapPointType.Shop);
        }

        for (int i = 0; i < _targetElites; i++)
        {
            types.Add(MapPointType.Elite);
        }

        for (int i = 0; i < _targetUnknowns; i++)
        {
            types.Add(MapPointType.Unknown);
        }

        _rng.Shuffle(types);
        return new Queue<MapPointType>(types);
    }

    private void AssignRemainingTypesToRandomPoints(Queue<MapPointType> pointTypesToAssign)
    {
        for (int pass = 0; pass < 4 && pointTypesToAssign.Count > 0; pass++)
        {
            List<MapPoint> candidates = GetAllMapPoints()
                .Where(static point => point.PointType == MapPointType.Unassigned)
                .ToList();
            _rng.Shuffle(candidates);

            foreach (MapPoint candidate in candidates)
            {
                if (pointTypesToAssign.Count == 0)
                {
                    break;
                }

                MapPointType type = GetNextValidPointType(pointTypesToAssign, candidate);
                if (type != MapPointType.Unassigned)
                {
                    candidate.PointType = type;
                }
            }
        }
    }

    private MapPointType GetNextValidPointType(Queue<MapPointType> pointTypesQueue, MapPoint mapPoint)
    {
        int count = pointTypesQueue.Count;
        for (int i = 0; i < count; i++)
        {
            MapPointType pointType = pointTypesQueue.Dequeue();
            if (IsValidPointType(pointType, mapPoint))
            {
                return pointType;
            }

            pointTypesQueue.Enqueue(pointType);
        }

        return MapPointType.Unassigned;
    }

    private bool IsValidPointType(MapPointType pointType, MapPoint mapPoint)
    {
        return IsValidForUpper(pointType, mapPoint)
            && IsValidForLower(pointType, mapPoint)
            && IsValidWithParents(pointType, mapPoint)
            && IsValidWithChildren(pointType, mapPoint)
            && IsValidWithSiblings(pointType, mapPoint);
    }

    private bool IsValidForLower(MapPointType pointType, MapPoint mapPoint)
    {
        return mapPoint.coord.row >= 6 || !LowerMapPointRestrictions.Contains(pointType);
    }

    private bool IsValidForUpper(MapPointType pointType, MapPoint mapPoint)
    {
        return mapPoint.coord.row < _mapLength - 3 || !UpperMapPointRestrictions.Contains(pointType);
    }

    private static bool IsValidWithParents(MapPointType pointType, MapPoint mapPoint)
    {
        return !ParentMapPointRestrictions.Contains(pointType)
            || !mapPoint.parents.Concat(mapPoint.Children).Any(other => other.PointType == pointType);
    }

    private static bool IsValidWithChildren(MapPointType pointType, MapPoint mapPoint)
    {
        return !ChildMapPointRestrictions.Contains(pointType)
            || !mapPoint.Children.Any(other => other.PointType == pointType);
    }

    private static bool IsValidWithSiblings(MapPointType pointType, MapPoint mapPoint)
    {
        return !SiblingPointTypeRestrictions.Contains(pointType)
            || !GetSiblings(mapPoint).Any(other => other.PointType == pointType);
    }

    private static IEnumerable<MapPoint> GetSiblings(MapPoint mapPoint)
    {
        return mapPoint.parents.SelectMany(static parent => parent.Children).Where(sibling => !ReferenceEquals(sibling, mapPoint));
    }

    private MapPoint GetOrCreatePoint(int col, int row)
    {
        MapPoint? existing = GetPoint(col, row);
        if (existing != null)
        {
            return existing;
        }

        MapPoint created = new(col, row);
        _grid[col, row] = created;
        return created;
    }

    private void ForEachInRow(int rowIndex, Action<MapPoint, RandomizedWiderActMap> processor, RandomizedWiderActMap self)
    {
        for (int col = 0; col < _mapWidth; col++)
        {
            MapPoint? point = _grid[col, rowIndex];
            if (point != null)
            {
                processor(point, self);
            }
        }
    }

    private bool ShouldReplaceTreasureWithElites(ActMap original)
    {
        int treasureRow = original.GetRowCount() - 7;
        if (treasureRow <= 1 || treasureRow >= original.GetRowCount())
        {
            return false;
        }

        List<MapPoint> rowPoints = original.GetPointsInRow(treasureRow).ToList();
        return rowPoints.Count > 0 && rowPoints.All(static point => point.PointType == MapPointType.Elite);
    }

    private (int extraRests, int shops, int elites, int unknowns) BuildScaledTargets(ActMap original)
    {
        int originalNodeCount = original.GetAllMapPoints().Count();
        double estimatedScale = Math.Max(1.35, (double)(_mapWidth * _mapLength) / Math.Max(1, original.GetColumnCount() * original.GetRowCount()) * 0.9);

        int originalExtraRests = Math.Max(0,
            original.GetAllMapPoints().Count(static point => point.PointType == MapPointType.RestSite)
            - original.GetPointsInRow(original.GetRowCount() - 1).Count(static point => point.PointType == MapPointType.RestSite));
        int originalShops = original.GetAllMapPoints().Count(static point => point.PointType == MapPointType.Shop);
        int originalElites = original.GetAllMapPoints().Count(static point => point.PointType == MapPointType.Elite);
        if (_shouldReplaceTreasureWithElites)
        {
            originalElites -= original.GetPointsInRow(original.GetRowCount() - 7).Count(static point => point.PointType == MapPointType.Elite);
        }

        int originalUnknowns = original.GetAllMapPoints().Count(static point => point.PointType == MapPointType.Unknown);

        int extraRests = Math.Max(1, (int)Math.Round(originalExtraRests * estimatedScale));
        int shops = Math.Max(3, (int)Math.Round(originalShops * estimatedScale));
        int elites = Math.Max(3, (int)Math.Round(originalElites * estimatedScale));
        int unknowns = Math.Max(8, (int)Math.Round(originalUnknowns * estimatedScale));

        int availableUnassigned = EstimateAvailableUnassignedPointCount();
        int totalSpecial = extraRests + shops + elites + unknowns;
        if (totalSpecial > availableUnassigned)
        {
            double reduction = (double)availableUnassigned / totalSpecial;
            extraRests = Math.Max(1, (int)Math.Floor(extraRests * reduction));
            shops = Math.Max(2, (int)Math.Floor(shops * reduction));
            elites = Math.Max(2, (int)Math.Floor(elites * reduction));
            unknowns = Math.Max(4, (int)Math.Floor(unknowns * reduction));
        }

        Entry.Logger.Info(
            $"[RandomizedWiderActMap] Target special counts from originalNodes={originalNodeCount}: rests={extraRests}, shops={shops}, elites={elites}, unknowns={unknowns}, scale={estimatedScale:F2}");

        return (extraRests, shops, elites, unknowns);
    }

    private int EstimateAvailableUnassignedPointCount()
    {
        int reservedRows = 2;
        if (_mapLength - 7 > 1)
        {
            reservedRows++;
        }

        return Math.Max(1, (_mapWidth * _mapLength / 2) - reservedRows * _mapWidth);
    }
}
