namespace WhatIfRelics.WhatIfRelicsCode;

public sealed class WhatIfRelicsSettings
{
    public bool EnableWhatIfRelics { get; set; } = false;

    public void ResetToDefaults()
    {
        WhatIfRelicsSettings defaults = new();
        EnableWhatIfRelics = defaults.EnableWhatIfRelics;
    }
}
