using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public interface IWhatIfUniformRelicSource
{
    RelicModel GetUniformRelic(IRunState runState);

    RelicModel? GetUniformRelicForHoverTips() => null;
}




