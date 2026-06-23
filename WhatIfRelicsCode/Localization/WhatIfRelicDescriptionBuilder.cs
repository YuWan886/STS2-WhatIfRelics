using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Interop;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Localization;

internal static class WhatIfRelicDescriptionBuilder
{
    private const string DynamicDescriptionTemplateKey = "WHAT_IF_RELICS_DYNAMIC_DESCRIPTION_TEMPLATE";

    public static LocString BuildCenteredLocString(WhatIfRelicModel relic)
    {
        LocString dynamicDescription = new("relics", DynamicDescriptionTemplateKey);
        dynamicDescription.Add("DynamicWhatIfDescription", WrapCentered(Build(relic)));
        return dynamicDescription;
    }

    public static LocString BuildLocString(WhatIfRelicModel relic)
    {
        LocString dynamicDescription = new("relics", DynamicDescriptionTemplateKey);
        dynamicDescription.Add("DynamicWhatIfDescription", Build(relic));
        return dynamicDescription;
    }

    public static LocString BuildOptionLocString(WhatIfRelicModel relic)
    {
        LocString dynamicDescription = new("relics", DynamicDescriptionTemplateKey);
        dynamicDescription.Add("DynamicWhatIfDescription", BuildOptionText(relic));
        return dynamicDescription;
    }

    public static string Build(WhatIfRelicModel relic)
    {
        string description = BuildOriginalDynamicDescription(relic).GetFormattedText();
        string? scope = BuildScopeDescription(relic);
        return AppendScopeDescription(description, scope);
    }

    public static string BuildOptionText(WhatIfRelicModel relic)
    {
        string description = TakeFirstSentence(BuildOriginalDynamicDescription(relic).GetFormattedText());
        string? scope = BuildScopeDescription(relic);
        return AppendScopeDescription(description, scope);
    }

    private static string WrapCentered(string text)
    {
        return $"[center]{text}[/center]";
    }

    private static LocString BuildOriginalDynamicDescription(WhatIfRelicModel relic)
    {
        LocString description = new("relics", relic.Id.Entry + ".description");
        relic.DynamicVars.AddTo(description);

        string prefix = EnergyIconHelper.GetPrefix(relic);
        description.Add("energyPrefix", prefix);
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");

        foreach (KeyValuePair<string, object> variable in description.Variables)
        {
            if (variable.Value is EnergyVar energyVar)
            {
                energyVar.ColorPrefix = prefix;
            }
        }

        return description;
    }

    private static string? BuildEffectDescription(WhatIfRelicModel relic)
    {
        return relic switch
        {
            WhatIfUniformCardRelicModel uniformCardRelic => BuildUniformCardEffect(uniformCardRelic),
            WhatIfBigBang => BuildSingleCardEffect(ModelDb.Card<BigBang>()),
            WhatIfWhistle => BuildSingleCardEffect(ModelDb.Card<Whistle>()),
            WhatIfDiscovery => BuildSingleCardEffect(ModelDb.Card<Discovery>()),
            WhatIfJackpot => BuildSingleCardEffect(ModelDb.Card<Jackpot>()),
            WhatIfSnakebite => BuildSingleCardEffect(ModelDb.Card<Snakebite>()),
            WhatIfDramaticEntrance => BuildSingleCardEffect(ModelDb.Card<DramaticEntrance>()),
            WhatIfSha => BuildShaEffect(),
            WhatIfStrike => BuildStrikeEffect(),
            WhatIfAncientCards => BuildAncientCardsEffect(),
            WhatIfTriplePlay triplePlay => BuildUniformRelicEffect(
                triplePlay,
                YuWanInterop.GetTriplePlayRelicEntry(),
                fallbackZhsTitle: "33",
                fallbackEngTitle: "Triple Play"),
            WhatIfSeriesRelics seriesRelics => BuildSeriesRelicsEffect(seriesRelics),
            WhatIfBingBong bingBong => BuildUniformRelicEffect(bingBong),
            WhatIfChemicalX chemicalX => BuildUniformRelicEffect(chemicalX),
            WhatIfDragonFruit dragonFruit => BuildUniformRelicEffect(dragonFruit),
            WhatIfHeartsteel heartsteel => BuildUniformRelicEffect(
                heartsteel,
                YuWanInterop.GetHeartsteelRelicEntry(),
                fallbackZhsTitle: "心之钢",
                fallbackEngTitle: "Heartsteel"),
            WhatIfHistoryCourse historyCourse => BuildUniformRelicEffect(historyCourse),
            WhatIfJump => BuildOriginalDynamicDescription(relic).GetFormattedText(),
            WhatIfOldCoin oldCoin => BuildUniformRelicEffect(oldCoin),
            WhatIfTenYearBamboo => BuildTenYearBambooEffect(),
            WhatIfWhiteStar whiteStar => BuildUniformRelicEffect(whiteStar),
            _ => null
        };
    }

    private static string? BuildScopeDescription(WhatIfRelicModel relic)
    {
        bool affectsStartingDeck = relic is WhatIfUniformCardRelicModel
            or WhatIfBigBang
            or WhatIfWhistle
            or WhatIfDiscovery
            or WhatIfJackpot
            or WhatIfSnakebite
            or WhatIfDramaticEntrance
            or WhatIfSha
            or WhatIfStrike
            or WhatIfAncientCards;

        bool affectsCardRewards = affectsStartingDeck;
        bool affectsRelicRewards = relic is IWhatIfUniformRelicSource;
        bool affectsPotionRewards = relic is IWhatIfUniformPotionSource;

        List<string> scopes = [];
        if (affectsStartingDeck && WhatIfReplacementContext.ShouldReplaceStartingDeck())
        {
            scopes.Add(GetText(zhs: "初始手牌", eng: "starting deck"));
        }

        if (affectsCardRewards && WhatIfReplacementContext.ShouldReplaceCardRewards(CardCreationSource.Encounter))
        {
            scopes.Add(GetText(zhs: "卡牌奖励", eng: "card rewards"));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceRelicRewards(null))
        {
            scopes.Add(GetText(zhs: "遗物奖励", eng: "relic rewards"));
        }

        if (affectsPotionRewards && WhatIfReplacementContext.ShouldReplacePotionRewards())
        {
            scopes.Add(GetText(zhs: "药水奖励", eng: "potion rewards"));
        }

        if (affectsCardRewards && WhatIfReplacementContext.ShouldReplaceCardRewards(CardCreationSource.Shop))
        {
            scopes.Add(GetText(zhs: "商店卡牌", eng: "shop cards"));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceShopRelics())
        {
            scopes.Add(GetText(zhs: "商店遗物", eng: "shop relics"));
        }

        if (affectsPotionRewards && WhatIfReplacementContext.ShouldReplaceShopPotions())
        {
            scopes.Add(GetText(zhs: "商店药水", eng: "shop potions"));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceTreasureRelics())
        {
            scopes.Add(GetText(zhs: "宝箱遗物", eng: "treasure chest relics"));
        }

        if (scopes.Count == 0)
        {
            return null;
        }

        return GetText(
            zhs: "替换范围：" + string.Join("、", scopes) + "。",
            eng: "Replacement scope: " + JoinEnglish(scopes) + ".");
    }

    private static string AppendScopeDescription(string description, string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return description;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return scope;
        }

        return description + "\n" + scope;
    }

    private static string BuildUniformCardEffect(WhatIfUniformCardRelicModel relic)
    {
        if (relic is WhatIfOnlyRare)
        {
            return BuildRandomizedCardEffect("稀有", "Rare");
        }

        if (relic is WhatIfOnlyUncommon)
        {
            return BuildRandomizedCardEffect("罕见", "Uncommon");
        }

        if (relic is WhatIfOnlyCommon)
        {
            return BuildRandomizedCardEffect("普通", "Common");
        }

        if (relic is WhatIfOnlyAttack)
        {
            return BuildRandomizedCardTypeEffect("攻击", "Attack");
        }

        if (relic is WhatIfOnlySkill)
        {
            return BuildRandomizedCardTypeEffect("技能", "Skill");
        }

        if (relic is WhatIfOnlyPower)
        {
            return BuildRandomizedCardTypeEffect("能力", "Power");
        }

        if (relic is WhatIfOnlyColorless)
        {
            return BuildRandomizedCardEffect("无色", "Colorless");
        }

        return BuildOriginalDynamicDescription(relic).GetFormattedText();
    }

    private static string BuildSingleCardEffect(CardModel card)
    {
        string title = card.Title;
        return BuildSingleCardEffect(title);
    }

    private static string BuildSingleCardEffect(string title)
    {
        return GetText(
            zhs: $"所有被替换的卡牌都会变为[gold]{title}[/gold]。",
            eng: $"All replaced cards become [gold]{title}[/gold].");
    }

    private static string BuildRandomizedCardEffect(string zhsCategory, string engCategory)
    {
        return GetText(
            zhs: $"所有被替换的卡牌都会变为随机的[gold]{zhsCategory}[/gold]牌。",
            eng: $"All replaced cards become random [gold]{engCategory}[/gold] cards.");
    }

    private static string BuildRandomizedCardTypeEffect(string zhsType, string engType)
    {
        return GetText(
            zhs: $"所有被替换的卡牌都会变为随机的[gold]{zhsType}[/gold]类牌。",
            eng: $"All replaced cards become random [gold]{engType}[/gold] cards.");
    }

    private static string BuildStrikeEffect()
    {
        string hellraiser = ModelDb.Card<Hellraiser>().Title;
        return GetText(
            zhs: $"所有被替换的卡牌会变化：第一张变为[gold]{hellraiser}[/gold]，其余变为随机的[gold]打击[/gold]卡牌。",
            eng: $"All replaced cards change as follows: the first becomes [gold]{hellraiser}[/gold], and the rest become random [gold]Strike[/gold] cards.");
    }

    private static string BuildShaEffect()
    {
        string title = GetResolvedCardTitle(
            YuWanInterop.GetShaCardEntry(),
            fallbackZhsTitle: "杀",
            fallbackEngTitle: "Sha");
        return BuildSingleCardEffect(title);
    }

    private static string BuildAncientCardsEffect()
    {
        return GetText(
            zhs: "移除卡组中的基础牌，并改为加入先古牌。",
            eng: "Removes the Basic cards from your deck and replaces them with Ancient cards.");
    }

    private static string BuildUniformRelicEffect(IWhatIfUniformRelicSource relicSource)
    {
        string relicTitle = relicSource.GetUniformRelicForHoverTips()?.Title.GetFormattedText()
            ?? (relicSource as RelicModel)?.HoverTipsExcludingRelic.OfType<HoverTip>().FirstOrDefault().Title
            ?? (relicSource as RelicModel)?.Title.GetFormattedText()
            ?? GetText("目标遗物", "the target relic");
        return GetText(
            zhs: $"所有被替换的遗物都会变为[gold]{relicTitle}[/gold]。",
            eng: $"All replaced relics become [gold]{relicTitle}[/gold].");
    }

    private static string BuildUniformRelicEffect(
        IWhatIfUniformRelicSource relicSource,
        string? entry,
        string fallbackZhsTitle,
        string fallbackEngTitle)
    {
        string fallbackTitle = GetResolvedRelicTitle(entry, fallbackZhsTitle, fallbackEngTitle);
        string relicTitle = relicSource.GetUniformRelicForHoverTips()?.Title.GetFormattedText()
            ?? (relicSource as RelicModel)?.HoverTipsExcludingRelic.OfType<HoverTip>().FirstOrDefault().Title
            ?? fallbackTitle;
        return GetText(
            zhs: $"所有被替换的遗物都会变为[gold]{relicTitle}[/gold]。",
            eng: $"All replaced relics become [gold]{relicTitle}[/gold].");
    }

    private static string BuildSeriesRelicsEffect(WhatIfSeriesRelics relic)
    {
        string relicTitle = relic.Title.GetFormattedText().Replace("假如只有", string.Empty).Trim();
        string fallbackTitle = string.IsNullOrWhiteSpace(relicTitle) ? GetText("七罪猪系列遗物", "Seven Sin Pig series relics") : relicTitle;
        return GetText(
            zhs: $"所有被替换的遗物都会变为[gold]{fallbackTitle}[/gold]。",
            eng: $"All replaced relics become [gold]{fallbackTitle}[/gold].");
    }

    private static string BuildTenYearBambooEffect()
    {
        string relicTitle = GetResolvedRelicTitle(
            YuWanInterop.GetTenYearBambooRelicEntry(),
            fallbackZhsTitle: "10年孤竹",
            fallbackEngTitle: "Ten Year Bamboo");
        return GetText(
            zhs: $"所有被替换的遗物都会变为[gold]{relicTitle}[/gold]。",
            eng: $"All replaced relics become [gold]{relicTitle}[/gold].");
    }

    private static string GetResolvedCardTitle(string? entry, string fallbackZhsTitle, string fallbackEngTitle)
    {
        string? title = YuWanInteropResolver.ResolveCardTitle(entry);
        return string.IsNullOrWhiteSpace(title) ? GetText(fallbackZhsTitle, fallbackEngTitle) : title;
    }

    private static string GetResolvedRelicTitle(string? entry, string fallbackZhsTitle, string fallbackEngTitle)
    {
        string? title = YuWanInteropResolver.ResolveRelicTitle(entry);
        return string.IsNullOrWhiteSpace(title) ? GetText(fallbackZhsTitle, fallbackEngTitle) : title;
    }

    private static string TakeFirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        string normalized = text.Replace("\r\n", "\n");
        int newlineIndex = normalized.IndexOf('\n');
        return newlineIndex >= 0 ? normalized[..newlineIndex] : normalized;
    }

    private static string GetFirstExtraHoverTipTitle(WhatIfRelicModel relic)
    {
        string? title = relic.HoverTipsExcludingRelic
            .OfType<HoverTip>()
            .Select(hoverTip => hoverTip.Title)
            .FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text));
        return string.IsNullOrWhiteSpace(title) ? relic.Title.GetFormattedText() : title;
    }

    private static string FormatScopeLine(
        IReadOnlyList<string> entries,
        string zhsPrefix,
        string zhsSuffix,
        string engPrefix,
        string engSuffix)
    {
        if (IsChinese())
        {
            return zhsPrefix + string.Join("、", entries) + zhsSuffix;
        }

        return engPrefix + JoinEnglish(entries) + engSuffix;
    }

    private static string JoinEnglish(IReadOnlyList<string> entries)
    {
        return entries.Count switch
        {
            0 => string.Empty,
            1 => entries[0],
            2 => $"{entries[0]} and {entries[1]}",
            _ => string.Join(", ", entries.Take(entries.Count - 1)) + ", and " + entries[^1]
        };
    }

    private static string GetText(string zhs, string eng)
    {
        return IsChinese() ? zhs : eng;
    }

    private static bool IsChinese()
    {
        return LocManager.Instance.Language == "zhs";
    }
}
