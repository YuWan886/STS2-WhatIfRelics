using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace WhatIfRelics.WhatIfRelicsCode.Powers;

public abstract class WhatIfPowerTemplate : ModPowerTemplate
{
    private const string PlaceholderIconPath = "res://WhatIfRelics/images/relics/what_if_placeholder.png";

    protected virtual string IconFileName => $"{Id.Entry.ToLowerInvariant()}.png";

    protected virtual string AutoIconPath => $"res://WhatIfRelics/images/powers/{IconFileName}";

    protected virtual string AutoBigIconPath => AutoIconPath;

    public sealed override PowerAssetProfile AssetProfile => new(
        IconPath: ResolveIconPath(AutoIconPath),
        BigIconPath: ResolveIconPath(AutoBigIconPath));

    private static string ResolveIconPath(string path)
    {
        return ResourceLoader.Exists(path) ? path : PlaceholderIconPath;
    }
}
