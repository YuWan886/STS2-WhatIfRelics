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

        return ModelDb.GetByIdOrNull<CardModel>(new ModelId("CARD", entry));
    }

    public static RelicModel? ResolveRelic(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry) || !YuWanInterop.IsAvailable())
        {
            return null;
        }

        return ModelDb.GetByIdOrNull<RelicModel>(new ModelId("RELIC", entry));
    }

    public static RelicModel[] ResolveRelics(IEnumerable<string> entries)
    {
        return entries
            .Select(ResolveRelic)
            .Where(relic => relic != null)
            .Cast<RelicModel>()
            .ToArray();
    }

    public static string? ResolveCardTitle(string? entry)
    {
        return ResolveCard(entry)?.Title;
    }

    public static string? ResolveRelicTitle(string? entry)
    {
        return ResolveRelic(entry)?.Title.GetFormattedText();
    }

    public static IEnumerable<IHoverTip> BuildCardHoverTips(string? entry)
    {
        return BuildCardHoverTips(ResolveCard(entry));
    }

    public static IEnumerable<IHoverTip> BuildCardHoverTips(CardModel? model)
    {
        return model == null
            ? []
            : [HoverTipFactory.FromCard(model), .. model.HoverTips];
    }

    public static IEnumerable<IHoverTip> BuildRelicHoverTips(string? entry)
    {
        return BuildRelicHoverTips(ResolveRelic(entry));
    }

    public static IEnumerable<IHoverTip> BuildRelicHoverTips(RelicModel? model)
    {
        return model?.HoverTips ?? [];
    }
}
