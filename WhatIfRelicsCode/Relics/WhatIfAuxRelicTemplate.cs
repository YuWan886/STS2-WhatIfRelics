using STS2RitsuLib.Scaffolding.Content;
using WhatIfRelics.WhatIfRelicsCode.Assets;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public abstract class WhatIfAuxRelicTemplate : ModRelicTemplate
{
    private const string RelicImageRoot = "res://WhatIfRelics/images/relics";

    public override RelicAssetProfile AssetProfile =>
        WhatIfAssetPathHelper.BuildRelicAssetProfile(GetType(), RelicImageRoot);
}
