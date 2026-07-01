using STS2RitsuLib.Scaffolding.Content;
using WhatIfRelics.WhatIfRelicsCode.Assets;

namespace WhatIfRelics.WhatIfRelicsCode.Powers;

public abstract class WhatIfPowerTemplate : ModPowerTemplate
{
    private const string PowerImageRoot = "res://WhatIfRelics/images/powers";

    protected virtual string IconFileName => WhatIfAssetPathHelper.BuildSnakeCasePngFileName(GetType());

    protected virtual string AutoIconPath => $"{PowerImageRoot}/{IconFileName}";

    protected virtual string AutoBigIconPath => AutoIconPath;

    public sealed override PowerAssetProfile AssetProfile => new(
        IconPath: ResolveIconPath(AutoIconPath),
        BigIconPath: ResolveIconPath(AutoBigIconPath));

    private static string ResolveIconPath(string path)
    {
        return WhatIfAssetPathHelper.ResolveExistingPath(path, WhatIfAssetPathHelper.PlaceholderRelicIconPath);
    }
}
