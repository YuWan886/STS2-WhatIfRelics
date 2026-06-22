using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using WhatIfRelics.WhatIfRelicsCode.Localization;

namespace WhatIfRelics.WhatIfRelicsCode;

public static class WhatIfRelicsSettingsPage
{
    public const string DataKey = "settings";

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> EnableWhatIfRelicsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.EnableWhatIfRelics,
        static (s, v) => s.EnableWhatIfRelics = v);

    public static WhatIfRelicsSettings Current =>
        RitsuLibFramework.GetDataStore(Entry.ModId).Get<WhatIfRelicsSettings>(DataKey);

    public static void Register()
    {
        ModDataStore.For(Entry.ModId).Register(
            key: DataKey,
            fileName: "settings.json",
            scope: SaveScope.Global,
            defaultFactory: static () => new WhatIfRelicsSettings(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(
            Entry.ModId,
            page => page
                .WithTitle(WhatIfRelicsLocalization.SettingsPageTitleText())
                .WithModDisplayName(WhatIfRelicsLocalization.ModTitleText())
                .WithVisibleOnHostSurfaces(ModSettingsHostSurface.All)
                .AddSection(
                    "general",
                    section => section
                        .WithTitle(WhatIfRelicsLocalization.GeneralSectionTitleText())
                        .AddToggle(
                            "enable_what_if_relics",
                            WhatIfRelicsLocalization.EnableAtStartText(),
                            EnableWhatIfRelicsBinding))
                .AddSection(
                    "reset",
                    section => section
                        .WithTitle(WhatIfRelicsLocalization.ResetSectionTitleText())
                        .AddButton(
                            "reset_defaults",
                            WhatIfRelicsLocalization.ResetButtonLabelText(),
                            WhatIfRelicsLocalization.ResetButtonText(),
                            ResetToDefaults,
                            ModSettingsButtonTone.Danger,
                            WhatIfRelicsLocalization.ResetButtonDescriptionText())));
    }

    private static void ResetToDefaults(IModSettingsUiActionHost host)
    {
        Current.ResetToDefaults();
        ModDataStore.For(Entry.ModId).Save(DataKey);
        host.RequestRefreshAfterDataModelBatchChange();
    }
}
