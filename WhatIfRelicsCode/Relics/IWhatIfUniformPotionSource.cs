using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public interface IWhatIfUniformPotionSource
{
    PotionModel GetUniformPotion(IRunState runState);

    PotionModel? GetUniformPotionForHoverTips() => null;
}
