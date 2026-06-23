using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfJump")]
public class WhatIfJump : WhatIfRelicModel
{
    private const int RequiredJumpCount = 10;
    private int _currentJumpCount;

    public override bool ShowCounter => true;

    public override int DisplayAmount => _currentJumpCount;

    public WhatIfJump() : base(true)
    {
    }

    public static bool HasJumpRelic(IRunState? runState)
    {
        return runState?.Players.Any(static player => player.Relics.Any(static relic => relic is WhatIfJump)) == true;
    }

    public void SetCurrentJumpCount(int jumpCount)
    {
        AssertMutable();

        int clamped = Math.Clamp(jumpCount, 0, RequiredJumpCount);
        if (_currentJumpCount == clamped)
        {
            return;
        }

        _currentJumpCount = clamped;
        InvokeDisplayAmountChanged();
    }
}
