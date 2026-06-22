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
        string? effect = BuildEffectDescription(relic);
        string? scope = BuildScopeDescription(relic);

        if (string.IsNullOrWhiteSpace(effect))
        {
            return BuildOriginalDynamicDescription(relic).GetFormattedText();
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            return effect;
        }

        return effect + "\n" + scope;
    }

    public static string BuildOptionText(WhatIfRelicModel relic)
    {
        string? effect = BuildEffectDescription(relic);
        if (!string.IsNullOrWhiteSpace(effect))
        {
            return effect;
        }

        return TakeFirstSentence(BuildOriginalDynamicDescription(relic).GetFormattedText());
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
            WhatIfOldCoin oldCoin => BuildUniformRelicEffect(oldCoin),
            WhatIfTenYearBamboo => BuildTenYearBambooEffect(),
            WhatIfWhiteStar whiteStar => BuildUniformRelicEffect(whiteStar),
            WhatIfNineLives => GetText(
                zhs: "当你将受到致命伤害时，改为存活且生命值变为[gold]1[/gold]。每局最多触发[gold]9[/gold]次。",
                eng: "When you would take fatal damage, survive instead and set your HP to [gold]1[/gold]. Triggers up to [gold]9[/gold] times per run."),
            WhatIfSellCards => GetText(
                zhs: "在[gold]商店[/gold]中可以右键牌组里的卡牌出售，售价为该卡商店购买价的[gold]50%[/gold]。",
                eng: "In [gold]shops[/gold], you can right-click cards in your deck to sell them for [gold]50%[/gold] of their shop buy price."),
            WhatIfInfinitePotions => GetText(
                zhs: "每当你拾取或使用药水后，都会用随机药水填满所有空药水栏位。",
                eng: "Whenever you procure or use a potion, fill all empty potion slots with random potions."),
            WhatIfSoloUpgrade => GetText(
                zhs: "每场战斗结束后，随机升级牌组中的[gold]1[/gold]张卡牌。",
                eng: "After each combat, randomly upgrade [gold]1[/gold] card in your deck."),
            WhatIfGoSecond => GetText(
                zhs: "每场战斗开始时，在玩家抽牌之后、首个玩家出牌阶段之前，敌人会立即先执行一次行动。",
                eng: "At the start of each combat, after the player draws but before the first player play phase, enemies immediately take one action."),
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
            scopes.Add(GetText(
                zhs: "拾起时替换初始手牌。",
                eng: "Replaces your starting deck when obtained."));
        }

        var rewardScopes = new List<string>();
        if (affectsCardRewards && WhatIfReplacementContext.ShouldReplaceCardRewards(CardCreationSource.Encounter))
        {
            rewardScopes.Add(GetText(zhs: "卡牌奖励", eng: "card rewards"));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceRelicRewards(null))
        {
            rewardScopes.Add(GetText(zhs: "遗物奖励", eng: "relic rewards"));
        }

        if (affectsPotionRewards && WhatIfReplacementContext.ShouldReplacePotionRewards())
        {
            rewardScopes.Add(GetText(zhs: "药水奖励", eng: "potion rewards"));
        }

        if (rewardScopes.Count > 0)
        {
            scopes.Add(FormatScopeLine(
                rewardScopes,
                zhsPrefix: "替换",
                zhsSuffix: "。",
                engPrefix: "Also replaces ",
                engSuffix: "."));
        }

        var merchantScopes = new List<string>();
        if (affectsCardRewards && WhatIfReplacementContext.ShouldReplaceCardRewards(CardCreationSource.Shop))
        {
            merchantScopes.Add(GetText(zhs: "商店卡牌", eng: "shop cards"));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceShopRelics())
        {
            merchantScopes.Add(GetText(zhs: "商店遗物", eng: "shop relics"));
        }

        if (affectsPotionRewards && WhatIfReplacementContext.ShouldReplaceShopPotions())
        {
            merchantScopes.Add(GetText(zhs: "商店药水", eng: "shop potions"));
        }

        if (merchantScopes.Count > 0)
        {
            scopes.Add(FormatScopeLine(
                merchantScopes,
                zhsPrefix: "替换",
                zhsSuffix: "。",
                engPrefix: "Also replaces ",
                engSuffix: "."));
        }

        if (affectsRelicRewards && WhatIfReplacementContext.ShouldReplaceTreasureRelics())
        {
            scopes.Add(GetText(
                zhs: "替换宝箱遗物。",
                eng: "Also replaces treasure chest relics."));
        }

        return scopes.Count == 0 ? null : string.Join("\n", scopes);
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
