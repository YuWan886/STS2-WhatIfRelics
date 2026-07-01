using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;
using WhatIfRelics.WhatIfRelicsCode.Assets;

namespace WhatIfRelics.WhatIfRelicsCode.Cards;

public abstract class WhatIfCardTemplate(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    protected virtual string PortraitFileName => WhatIfAssetPathHelper.BuildSnakeCasePngFileName(GetType());

    protected virtual string AutoPortraitPath => $"{Entry.ResPath}/images/card_portraits/{PortraitFileName}";

    protected virtual string? BetaPortraitFileName => null;

    protected virtual string? AutoBetaPortraitPath => string.IsNullOrWhiteSpace(BetaPortraitFileName)
        ? null
        : $"{Entry.ResPath}/images/card_portraits/{BetaPortraitFileName}";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: ResolvePortraitPath(AutoPortraitPath),
        BetaPortraitPath: ResolvePortraitPath(AutoBetaPortraitPath));

    private static string? ResolvePortraitPath(string? path)
    {
        return WhatIfAssetPathHelper.ResolveExistingPathOrNull(path);
    }
}
