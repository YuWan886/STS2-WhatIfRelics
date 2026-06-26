using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
internal static class WhatIfMerchantInventoryCompatPatch
{
    [HarmonyPrefix]
    private static void NMerchantInventory_Initialize_Prefix(NMerchantInventory __instance, MerchantInventory inventory)
    {
        if (__instance.GetNodeOrNull<Control>("%Relics") is not Control relicContainer)
        {
            return;
        }

        int missingSlots = inventory.RelicEntries.Count - relicContainer.GetChildCount();
        if (missingSlots <= 0)
        {
            return;
        }

        if (relicContainer.GetChildOrNull<NMerchantRelic>(relicContainer.GetChildCount() - 1) is not NMerchantRelic template)
        {
            return;
        }

        Vector2 offset = GetNextSlotOffset(relicContainer);
        for (int i = 0; i < missingSlots; i++)
        {
            if (template.Duplicate() is not NMerchantRelic extraSlot)
            {
                return;
            }

            extraSlot.Name = $"{template.Name}_WhatIfCompat{relicContainer.GetChildCount()}";
            extraSlot.Position = template.Position + offset * (i + 1);
            relicContainer.AddChild(extraSlot);
        }
    }

    private static Vector2 GetNextSlotOffset(Control relicContainer)
    {
        int childCount = relicContainer.GetChildCount();
        if (childCount >= 2
            && relicContainer.GetChildOrNull<NMerchantRelic>(childCount - 1) is { } last
            && relicContainer.GetChildOrNull<NMerchantRelic>(childCount - 2) is { } previous)
        {
            Vector2 offset = last.Position - previous.Position;
            if (offset.LengthSquared() > 1f)
            {
                return offset;
            }
        }

        return new Vector2(160f, 0f);
    }
}
