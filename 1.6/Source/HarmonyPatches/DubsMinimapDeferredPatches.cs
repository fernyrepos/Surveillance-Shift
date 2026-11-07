using System;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    public static class DubsMinimapPatchGate
    {
        public static bool Ready;
    }

    [StaticConstructorOnStartup]
    public static class DubsMinimapDeferredPatches
    {
        static DubsMinimapDeferredPatches()
        {
            if (ModsConfig.IsActive("dubwise.dubsmintminimap") is false) return;
            DubsMinimapPatchGate.Ready = true;
            try
            {
                SurveillanceShiftMod.harmony.CreateClassProcessor(typeof(DubsMinimap_Fogged_Patch)).Patch();
                SurveillanceShiftMod.harmony.CreateClassProcessor(typeof(DubsMinimap_FoggedCell_Patch)).Patch();
            }
            catch (Exception e)
            {
                Log.Warning("[Surveillance Shift] Could not patch Dubs Mint Minimap: " + e.Message);
            }
        }
    }
}
