using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Interop;

internal static class YuWanInteropResolver
{
    public static CardModel? ResolveCard(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry) || !YuWanInterop.IsAvailable())
        {
            return null;
        }

        return ModelDb.GetById<CardModel>(new ModelId("CARD", entry));
    }

    public static RelicModel? ResolveRelic(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry) || !YuWanInterop.IsAvailable())
        {
            return null;
        }

        return ModelDb.GetById<RelicModel>(new ModelId("RELIC", entry));
    }

    public static RelicModel[] ResolveRelics(IEnumerable<string> entries)
    {
        return entries
            .Select(ResolveRelic)
            .Where(relic => relic != null)
            .Cast<RelicModel>()
            .ToArray();
    }

    public static IEnumerable<IHoverTip> BuildCardHoverTips(CardModel? model)
    {
        return model == null
            ? []
            : [HoverTipFactory.FromCard(model), .. model.HoverTips];
    }

    public static IEnumerable<IHoverTip> BuildRelicHoverTips(RelicModel? model)
    {
        return model?.HoverTips ?? [];
    }
}
